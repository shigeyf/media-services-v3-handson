using System;

namespace amsv3functions
{
    public class ServiceClientCredentialsConfig
    {
        public Uri AadEndpoint
        {
            get { return new Uri(Environment.GetEnvironmentVariable("AadEndpoint")); }
        }

        public string AadTenantId
        {
            get { return Environment.GetEnvironmentVariable("AadTenantId"); }
        }

        public string AadClientId
        {
            get { return Environment.GetEnvironmentVariable("AadClientId"); }
        }

        public string AadClientSecret
        {
            get { return Environment.GetEnvironmentVariable("AadClientSecret"); }
        }

        public Uri ArmEndpoint
        {
            get { return new Uri(Environment.GetEnvironmentVariable("ArmEndpoint")); }
        }

        public Uri ArmAadAudience
        {
            get { return new Uri(Environment.GetEnvironmentVariable("ArmAadAudience")); }
        }
    }
}
