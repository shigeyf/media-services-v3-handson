//
// Azure Media Services REST API v3 - Functions
//
// create_empty_asset - This function creates an empty asset.
//
//  Input:
//      {
//          "assetName":  "Name of the asset",
//          "assetStorage":  "Name of attached storage where to create the asset"  // (optional)  
//      }
//  Output:
//      {
//          "assetId":  "Id of the asset created"
//      }
//

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

using Newtonsoft.Json;


namespace amsv3functions
{
    public static class create_empty_asset
    {
        [FunctionName("create_empty_asset")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - create_empty_asset was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            if (data.assetName == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Please pass assetName in the input object"
                });
            }
            string assetName = data.assetName;
            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset asset = null;

            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                asset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, assetName, new Asset());
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
                assetId = asset.AssetId
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
