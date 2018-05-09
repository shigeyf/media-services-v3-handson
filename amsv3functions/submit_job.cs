//
// Azure Media Services REST API v3 - Functions
//
// submit_job - This function submits Media job in AMS account.
//
//  Input:
//      {
//          "inputAssetName":  "Name of the asset for input",
//          "outputAssetName":  "Name of the asset for output",
//          "transformName":  "Name of the Transform"
//      }
//  Output:
//      {
//          "jobName":  "Name of media Job"
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
    public static class submit_job
    {
        [FunctionName("submit_job")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - create_transform was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.inputAssetName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass inputAssetName in the input object" });
            if (data.outputAssetName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass outputAssetName in the input object" });
            if (data.transformName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass transformName in the input object" });
            string inputAssetName = data.inputAssetName;
            string outputAssetName = data.outputAssetName;
            string transformName = data.transformName;

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset inputAsset = null;
            Asset outputAsset = null;

            string guid = Guid.NewGuid().ToString();
            string jobName = "amsv3function-job-" + guid;


            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);

                inputAsset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, inputAssetName);
                if (inputAsset == null)
                    return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset for input not found" });
                outputAsset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, outputAssetName);
                if (outputAsset == null)
                    return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset for output not found" });

                // Use the name of the created input asset to create the job input.
                JobInput jobInput = new JobInputAsset(assetName: inputAssetName);
                JobOutput[] jobOutputs = { new JobOutputAsset(outputAssetName) };
                Job job = client.Jobs.Create(
                    amsconfig.ResourceGroup,
                    amsconfig.AccountName,
                    transformName,
                    jobName,
                    new Job
                    {
                        Input = jobInput,
                        Outputs = jobOutputs,
                    }
                );
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
                jobName = jobName
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
