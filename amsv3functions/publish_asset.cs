//
// Azure Media Services REST API v3 - Functions
//
// publish_asset - This function publishes an asset in AMS account.
//
//  Input:
//      {
//          "publishAssetName":  "Name of the asset for output",
//          "streamingPolicy": "Name of StreamingPolicy" // (Optional) default = "ClearStreamingOnly"
//          "streamingEndpointName": "default" // (Optional) default = "default"
//      }
//  Output:
//      {
//      }
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


namespace amsv3functions
{
    public static class publish_asset
    {
        private static Dictionary<string, PredefinedStreamingPolicy> predefinedStreamingPolicy = new Dictionary<string, PredefinedStreamingPolicy>()
        {
            { "ClearKey", PredefinedStreamingPolicy.ClearKey },
            { "ClearStreamingOnly", PredefinedStreamingPolicy.ClearStreamingOnly },
            { "DownloadAndClearStreaming", PredefinedStreamingPolicy.DownloadAndClearStreaming },
            { "DownloadOnly", PredefinedStreamingPolicy.DownloadOnly },
            { "SecureStreaming", PredefinedStreamingPolicy.SecureStreaming },
            { "SecureStreamingWithFairPlay", PredefinedStreamingPolicy.SecureStreamingWithFairPlay }
        };

        [FunctionName("publish_asset")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - publish_asset was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.publishAssetName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass publishAssetName in the input object" });
            string publishAssetName = data.publishAssetName;
            PredefinedStreamingPolicy streamingPolicy = PredefinedStreamingPolicy.ClearStreamingOnly; // default
            if (data.streamingPolicy != null)
            {
                string streamingPolicyName = data.streamingPolicy;
                if (predefinedStreamingPolicy.ContainsKey(streamingPolicyName))
                    streamingPolicy = predefinedStreamingPolicy[streamingPolicyName];
            }
            string streamingEndpointName = "default"; // default
            if (data.streamingEndpointName != null)
                streamingEndpointName = data.streamingEndpointName;

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            string guid = Guid.NewGuid().ToString();
            string locatorName = "locator-" + guid;
            PublishAssetOutput output = null;

            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                StreamingLocator locator =
                    client.StreamingLocators.Create(amsconfig.ResourceGroup,
                    amsconfig.AccountName,
                    locatorName,
                    new StreamingLocator()
                    {
                        AssetName = publishAssetName,
                        StreamingPolicyName = streamingPolicy,
                    });

                string streamingUrlPrefx = "";
                StreamingEndpoint streamingEndpoint = client.StreamingEndpoints.Get(amsconfig.ResourceGroup, amsconfig.AccountName, streamingEndpointName);
                if (streamingEndpoint != null)
                    streamingUrlPrefx = streamingEndpoint.HostName;
                ListPathsResponse paths = client.StreamingLocators.ListPaths(amsconfig.ResourceGroup, amsconfig.AccountName, locatorName);
                output = MediaServicesHelper.ConvertToPublishAssetOutput(locatorName, streamingUrlPrefx, paths);
            }
            catch (ApiErrorException e)
            {
                log.Info($"ERROR: AMS API call failed with error code: {e.Body.Error.Code} and message: {e.Body.Error.Message}");
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "AMS API call error: " + e.Message
                });
            }

            return req.CreateResponse(HttpStatusCode.OK, output);
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
