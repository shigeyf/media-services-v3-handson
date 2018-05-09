//
// Azure Media Services REST API v3 - Functions
//
// monitor_job_status - This function monitors Media job status in AMS account.
//
//  Input:
//      {
//          "jobName":  "Name of media Job",
//          "transformName":  "Name of the Transform"
//      }
//  Output:
//      {
//          "jobStatus": "Status of Job"
//          //  "Canceled"      JobState.Canceled
//          //  "Canceling"     JobState.Canceling
//          //  "Error"         JobState.Error
//          //  "Finished"      JobState.Finished
//          //  "Processing"    JobState.Processing
//          //  "Queued"        JobState.Queued
//          //  "Scheduled"     JobState.Scheduled
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
    public static class monitor_job_status
    {
        [FunctionName("monitor_job_status")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"AMS v3 Function - monitor_job_status was triggered!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // Validate input objects
            if (data.jobName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass jobName in the input object" });
            if (data.transformName == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass transformName in the input object" });
            string jobName = data.jobName;
            string transformName = data.transformName;

            MediaServicesConfigWrapper amsconfig = new MediaServicesConfigWrapper();
            Job job = null;

            try
            {
                IAzureMediaServicesClient client = CreateMediaServicesClient(amsconfig);
                job = client.Jobs.Get(amsconfig.ResourceGroup, amsconfig.AccountName, transformName, jobName);
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
                // job status
                jobStatus = job.State
                // job output progress
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
