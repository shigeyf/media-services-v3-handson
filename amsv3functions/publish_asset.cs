//
// Azure Media Services REST API v3 - Functions
//
// publish_asset - This function publishes an asset in AMS account.
//
//  Input:
//      {
//          "publishAssetName":  "Name of the asset for output"
//      }
//  Output:
//      {
//      }
//

using System;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;


namespace amsv3functions
{
    public static class publish_asset
    {
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

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            string guid = Guid.NewGuid().ToString();
            string locatorName = "locator-" + guid;

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
                        StreamingPolicyName = PredefinedStreamingPolicy.ClearStreamingOnly,
                    });
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
                output = ""
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
