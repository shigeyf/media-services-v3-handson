//
// Azure Media Services REST API v3 - Functions
//
// create_empty_asset - This function creates an empty asset.
//
//  Input:
//      {
//          "assetName":  "Name of the asset",
//          "assetId":  "Id of the asset created",
//          "sourceStorageAccountName":  "",
//          "sourceStorageAccountKey":  "",
//          "sourceContainer":  "",
//          "fileNames":  [ "filename.mp4" , "filename2.mp4"]
//      }
//  Output:
//      {
//          "destinationContainer": "asset-2e26fd08-1436-44b1-8b92-882a757071dd" // container of asset
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
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace amsv3functions
{
    public static class start_blob_copy_to_asset
    {
        [FunctionName("start_blob_copy_to_asset")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - start_blob_copy_to_asset was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.assetName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass assetName in the input object" });
            if (data.assetId == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass assetId in the input object" });
            if (data.sourceStorageAccountName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass sourceStorageAccountName in the input object" });
            if (data.sourceStorageAccountKey == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass sourceStorageAccountKey in the input object" });
            if (data.sourceContainer == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass sourceContainer in the input object" });
            log.Info("Input - assetName : " + data.assetName);
            log.Info("Input - assetId : " + data.assetId);
            log.Info("Input - SourceStorageAccountName : " + data.sourceStorageAccountName);
            log.Info("Input - SourceStorageAccountKey : " + data.sourceStorageAccountKey);
            string assetName = data.assetName;
            string assetId = data.assetId;
            string _sourceStorageAccountName = data.sourceStorageAccountName;
            string _sourceStorageAccountKey = data.sourceStorageAccountKey;
            string sourceContainerName = data.sourceContainer;
            List<string> fileNames = null;
            if (data.fileNames != null)
            {
                fileNames = ((JArray)data.fileNames).ToObject<List<string>>();
            }

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset asset = null;

            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                asset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, assetName);
                if (asset == null)
                    return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset not found" });

                // Setup blob container
                CloudBlobContainer sourceBlobContainer = BlobStorageHelper.GetCloudBlobContainer(_sourceStorageAccountName, _sourceStorageAccountKey, sourceContainerName);
                sourceBlobContainer.CreateIfNotExists();
                var response = client.Assets.ListContainerSas(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, permissions: AssetContainerPermission.ReadWrite, expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());
                var sasUri = new Uri(response.AssetContainerSasUrls.First());
                CloudBlobContainer destinationBlobContainer = new CloudBlobContainer(sasUri);

                // Copy Source Blob container into Destination Blob container that is associated with the asset.
                BlobStorageHelper.CopyBlobsAsync(sourceBlobContainer, destinationBlobContainer, fileNames, log);
            }
            catch (ApiErrorException e)
            {
                log.Info($"ERROR: AMS API call failed with error code: {e.Body.Error.Code} and message: {e.Body.Error.Message}");
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "AMS API call error: " + e.Message
                });
            }

            return req.CreateResponse(HttpStatusCode.OK, new
            {
                destinationContainer = $"asset-{data.assetId}"
            });
        }

        private static IAzureMediaServicesClient CreateMediaServicesClient(MediaServicesConfigWrapper config)
        {
            ArmClientCredentials credentials = new ArmClientCredentials(config.serviceClientCredentialsConfig);

            return new AzureMediaServicesClient(config.serviceClientCredentialsConfig.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }
    }
}
