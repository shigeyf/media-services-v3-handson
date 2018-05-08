using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;


namespace amsv3functions
{
    public class BlobStorageHelper
    {
        static public CloudBlobContainer GetCloudBlobContainer(string storageAccountName, string storageAccountKey, string containerName)
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            return cloudBlobClient.GetContainerReference(containerName);
        }

        static public void CopyBlobsAsync(CloudBlobContainer sourceBlobContainer, CloudBlobContainer destinationBlobContainer, List<string> fileNames, TraceWriter log)
        {
            string blobPrefix = null;
            bool useFlatBlobListing = true;
            if (fileNames != null)
            {
                log.Info("Copying listed blob files...");
                foreach (var fileName in fileNames)
                {
                    CloudBlob sourceBlob = sourceBlobContainer.GetBlockBlobReference(fileName);
                    log.Info("Source blob : " + (sourceBlob as CloudBlob).Uri.ToString());
                    CloudBlob destinationBlob = destinationBlobContainer.GetBlockBlobReference(fileName);
                    if (destinationBlob.Exists())
                    {
                        log.Info("Destination blob already exists. Skipping: " + destinationBlob.Uri.ToString());
                    }
                    else
                    {
                        log.Info("Copying blob " + sourceBlob.Uri.ToString() + " to " + destinationBlob.Uri.ToString());
                        CopyBlobAsync(sourceBlob as CloudBlob, destinationBlob);
                    }
                }
            }
            else
            {
                log.Info("Copying all blobs in the source container...");
                var blobList = sourceBlobContainer.ListBlobs(blobPrefix, useFlatBlobListing, BlobListingDetails.None);
                foreach (var sourceBlob in blobList)
                {
                    log.Info("Source blob : " + (sourceBlob as CloudBlob).Uri.ToString());
                    CloudBlob destinationBlob = destinationBlobContainer.GetBlockBlobReference((sourceBlob as CloudBlob).Name);
                    if (destinationBlob.Exists())
                    {
                        log.Info("Destination blob already exists. Skipping: " + destinationBlob.Uri.ToString());
                    }
                    else
                    {
                        log.Info("Copying blob " + sourceBlob.Uri.ToString() + " to " + destinationBlob.Uri.ToString());
                        CopyBlobAsync(sourceBlob as CloudBlob, destinationBlob);
                    }
                }
            }
        }

        static public async void CopyBlobAsync(CloudBlob sourceBlob, CloudBlob destinationBlob)
        {
            var signature = sourceBlob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24)
            });
            await destinationBlob.StartCopyAsync(new Uri(sourceBlob.Uri.AbsoluteUri + signature));
        }

        static public CopyStatus MonitorBlobContainer(CloudBlobContainer destinationBlobContainer)
        {
            string blobPrefix = null;
            bool useFlatBlobListing = true;
            var destBlobList = destinationBlobContainer.ListBlobs(blobPrefix, useFlatBlobListing, BlobListingDetails.Copy);
            CopyStatus copyStatus = CopyStatus.Success;
            foreach (var dest in destBlobList)
            {
                var destBlob = dest as CloudBlob;
                if (destBlob.CopyState.Status == CopyStatus.Aborted || destBlob.CopyState.Status == CopyStatus.Failed)
                {
                    // Log the copy status description for diagnostics and restart copy
                    destBlob.StartCopyAsync(destBlob.CopyState.Source);
                    copyStatus = CopyStatus.Pending;
                }
                else if (destBlob.CopyState.Status == CopyStatus.Pending)
                {
                    // We need to continue waiting for this pending copy
                    // However, let us log copy state for diagnostics
                    copyStatus = CopyStatus.Pending;
                }
                // else we completed this pending copy
            }
            return copyStatus;
        }
    }
}
