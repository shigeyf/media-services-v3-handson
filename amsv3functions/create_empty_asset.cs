//
//
//

using Newtonsoft.Json;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace amsv3functions
{
    public static class create_empty_asset
    {
        private const string AdaptiveStreamingTransformName = "MyTransformWithAdaptiveStreamingPreset";

        [FunctionName("create_empty_asset")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"Webhook was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            if (data.first == null || data.last == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Please pass first/last properties in the input object"
                });
            }

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                Transform transform = EnsureTransformExists(client, amsconfig.ResourceGroup, amsconfig.AccountName, AdaptiveStreamingTransformName);
                log.Info("OK");
            }
            catch (ApiErrorException e)
            {
                log.Info("Error");
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "AMS Call Error"
                });

                //log.Info("{0}", ex.Message);

                //log.Info("ERROR:API call failed with error code: {0} and message: {1}", ex.Body.Error.Code, ex.Body.Error.Message);
            }


            return req.CreateResponse(HttpStatusCode.OK, new
            {
                greeting = $"Hello {data.first} {data.last}!"
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

        private static Transform EnsureTransformExists(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string transformName)
        {

            // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
            // also uses the same recipe or Preset for processing content.
            Transform transform = client.Transforms.Get(resourceGroupName, accountName, transformName);

            if (transform == null)
            {
                // You need to specify what you want it to produce as an output
                TransformOutput[] output = new TransformOutput[]
                {
                    new TransformOutput
                    {
                        // The preset for the Transform is set to one of Media Services built-in sample presets.
                        // You can  customize the encoding settings by changing this to use "StandardEncoderPreset" class.
                        Preset = new BuiltInStandardEncoderPreset()
                        {
                            // This sample uses the built-in encoding preset for Adaptive Bitrate Streaming.
                            PresetName = EncoderNamedPreset.AdaptiveStreaming
                        }
                    }
                };

                // Create the Transform with the output defined above
                transform = client.Transforms.CreateOrUpdate(resourceGroupName, accountName, transformName, output);
            }

            return transform;
        }
    }
}
