//
// Azure Media Services REST API v3 - Functions
//
// monitor_blob_copy_container_status - This function monitors blob copy.
//
//  Input:
//      {
//          "destinationContainer":  "Name of the destination container for the asset",
//          "fileNames":  [ "filename.mp4" , "filename2.mp4"],
//          "delay": "180"  // (Optional)
//      }
//  Output:
//      {
//          "copyStatus": true, // Return Blob Copy Status: true or false
//          "blobCopyStatus": [
//              {
//                  "blobName": "Name of blob",
//                  "blobCopyStatus": 2  // Return Blob CopyStatus (see below)
//              }
//          ]
//          // https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage.blob.copystatus?view=azure-dotnet
//          //      Invalid     0	The copy status is invalid.
//          //      Pending     1	The copy operation is pending.
//          //      Success     2	The copy operation succeeded.
//          //      Aborted     3	The copy operation has been aborted.
//          //      Failed      4	The copy operation encountered an error.
//      }
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace amsv3functions
{
    public static class monitor_blob_copy_container_status
    {
        // Read values from the App.config file.
        private static readonly string _storageAccountName = Environment.GetEnvironmentVariable("MediaServicesStorageAccountName");
        private static readonly string _storageAccountKey = Environment.GetEnvironmentVariable("MediaServicesStorageAccountKey");

        [FunctionName("monitor_blob_copy_container_status")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - monitor_blob_copy_container_status was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.destinationContainer == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass destinationContainer in the input object" });
            string destinationContainerName = data.destinationContainer;
            List<string> fileNames = null;
            if (data.fileNames != null)
            {
                fileNames = ((JArray)data.fileNames).ToObject<List<string>>();
            }

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            bool copyStatus = true;
            //CopyStatus copyStatus = CopyStatus.Success;

            try
            {
                CloudBlobContainer destinationBlobContainer = BlobStorageHelper.GetCloudBlobContainer(_storageAccountName, _storageAccountKey, destinationContainerName);

                string blobPrefix = null;
                bool useFlatBlobListing = true;
                var destBlobList = destinationBlobContainer.ListBlobs(blobPrefix, useFlatBlobListing, BlobListingDetails.Copy);
                foreach (var dest in destBlobList)
                {
                    var destBlob = dest as CloudBlob;
                    if (destBlob.CopyState.Status == CopyStatus.Aborted || destBlob.CopyState.Status == CopyStatus.Failed)
                    {
                        // Log the copy status description for diagnostics and restart copy
                        destBlob.StartCopyAsync(destBlob.CopyState.Source);
                        //copyStatus = CopyStatus.Pending;
                        copyStatus = false;
                    }
                    else if (destBlob.CopyState.Status == CopyStatus.Pending)
                    {
                        // We need to continue waiting for this pending copy
                        // However, let us log copy state for diagnostics
                        //copyStatus = CopyStatus.Pending;
                        copyStatus = false;
                    }
                    // else we completed this pending copy
                }
            }
            catch (Exception e)
            {
                log.Info($"ERROR: Exception {e}");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            return req.CreateResponse(HttpStatusCode.OK, new
            {
                CopyStatus = copyStatus
            });
        }
    }
}
