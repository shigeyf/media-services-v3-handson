//
// Azure Media Services REST API v3 - Functions
//
// submit_job - This function submits Media job in AMS account.
//
//  Input:
//      {
//          "inputAssetName":  "Name of the asset for input",
//          "transformName":  "Name of the Transform",
//          "outputAssetNamePrefix":  "Name of the asset for output"
//          "assetStorageAccount":  "Name of attached storage where to create the asset"  // (optional)  
//      }
//  Output:
//      {
//          "jobName":  "Name of media Job"
//          "encoderOutputAssetName": "string",
//          "videoAnalyzerOutputAssetName": "string"
//      }
//

using System;
using System.Collections.Generic;
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
            log.Info($"AMS v3 Function - submit_job was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.inputAssetName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass inputAssetName in the input object" });
            if (data.transformName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass transformName in the input object" });
            if (data.outputAssetNamePrefix == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass outputAssetNamePrefix in the input object" });
            string inputAssetName = data.inputAssetName;
            string transformName = data.transformName;
            string outputAssetNamePrefix = data.outputAssetNamePrefix;
            string assetStorageAccount = null;
            if (data.assetStorageAccount != null)
                assetStorageAccount = data.assetStorageAccount;

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Asset inputAsset = null;

            string guid = Guid.NewGuid().ToString();
            string jobName = "amsv3function-job-" + guid;
            string encoderOutputAssetName = null;
            string videoAnalyzerOutputAssetName = null;

            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);

                inputAsset = client.Assets.Get(amsconfig.ResourceGroup, amsconfig.AccountName, inputAssetName);
                if (inputAsset == null)
                    return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset for input not found" });
                Transform transform = client.Transforms.Get(amsconfig.ResourceGroup, amsconfig.AccountName, transformName);
                if (transform == null)
                    return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Transform not found" });

                var jobOutputList = new List<JobOutput>();
                for (int i = 0; i < transform.Outputs.Count; i++)
                {
                    Guid assetGuid = Guid.NewGuid();
                    string outputAssetName = outputAssetNamePrefix + "-" + assetGuid.ToString();
                    Preset p = transform.Outputs[i].Preset;
                    if (p is BuiltInStandardEncoderPreset || p is StandardEncoderPreset)
                        encoderOutputAssetName = outputAssetName;
                    else if (p is VideoAnalyzerPreset)
                        videoAnalyzerOutputAssetName = outputAssetName;
                    Asset assetParams = new Asset(null, outputAssetName, null, assetGuid, DateTime.Now, DateTime.Now, null, outputAssetName, null, assetStorageAccount, AssetStorageEncryptionFormat.None);
                    Asset outputAsset = client.Assets.CreateOrUpdate(amsconfig.ResourceGroup, amsconfig.AccountName, outputAssetName, assetParams);
                    jobOutputList.Add(new JobOutputAsset(outputAssetName));
                }

                // Use the name of the created input asset to create the job input.
                JobInput jobInput = new JobInputAsset(assetName: inputAssetName);
                JobOutput[] jobOutputs = jobOutputList.ToArray();
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
                jobName = jobName,
                encoderOutputAssetName = encoderOutputAssetName,
                videoAnalyzerOutputAssetName = videoAnalyzerOutputAssetName
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
