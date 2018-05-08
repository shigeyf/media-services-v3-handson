using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace amsv3functions
{
    public class MediaServicesConfigWrapper
    {
        public ServiceClientCredentialsConfig serviceClientCredentialsConfig = new ServiceClientCredentialsConfig();

        public string SubscriptionId
        {
            get { return Environment.GetEnvironmentVariable("SubscriptionId"); }
        }

        public string ResourceGroup
        {
            get { return Environment.GetEnvironmentVariable("ResourceGroup"); }
        }

        public string AccountName
        {
            get { return Environment.GetEnvironmentVariable("AccountName"); }
        }

        public string Region
        {
            get { return Environment.GetEnvironmentVariable("Region"); }
        }
    }
}
