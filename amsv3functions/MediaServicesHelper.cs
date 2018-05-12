using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;


namespace amsv3functions
{
    public class MediaServicesHelper
    {
        static public PublishAssetOutput ConvertToPublishAssetOutput(string locatorName, string streamingUrlPrefx, ListPathsResponse paths)
        {
            PublishAssetOutput output = new PublishAssetOutput();

            output.locatorName = locatorName;

            List<PublishStreamingUrls> psUrls = new List<PublishStreamingUrls>();
            foreach (var path in paths.StreamingPaths)
            {
                var s = new PublishStreamingUrls();
                s.streamingProtocol = path.StreamingProtocol;
                s.encryptionScheme = path.EncryptionScheme;
                s.urls = new string[path.Paths.Count];
                for (int i = 0; i < path.Paths.Count; i++) s.urls[i] = "https://" + streamingUrlPrefx + path.Paths[i];
                if (path.StreamingProtocol == "SmoothStreaming")
                    output.streamingUrl = "https://" + streamingUrlPrefx + path.Paths[0];
                psUrls.Add(s);
            }
            output.streamingUrls = psUrls.ToArray();

            List<string> dUrls = new List<string>();
            foreach (var path in paths.DownloadPaths)
            {
                dUrls.Add("https://" + streamingUrlPrefx + path);
            }
            output.downloadUrls = dUrls.ToArray();

            return output;
        }
    }

    public class PublishStreamingUrls
    {
        public string streamingProtocol;
        public string encryptionScheme;
        public string[] urls;
    }

    public class PublishAssetOutput
    {
        public string locatorName;
        public string streamingUrl;
        public PublishStreamingUrls[] streamingUrls;
        public string[] downloadUrls;
    }
}
