using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    public static class GitHelper
    {
        public static string Version { get; } = "N/A";

        public static string FormatVersion(this string rawVersion)
        {
            return $"{rawVersion.Substring(0, 6)}";
        }

        static GitHelper()
        {
            var gitDir = "../.git";
            if (Directory.Exists(gitDir))
            {
                // Local, running in a repo directory.
                var head = File.ReadAllText($"{gitDir}/HEAD");

                if(head.StartsWith("ref: "))
                {
                    var refFile = head.Substring(5).TrimEnd('\n');
                    Version = File.ReadAllText($"{gitDir}/{refFile}").FormatVersion();
                }
                else
                {
                    Version = head.FormatVersion();
                }
            }

            var activeAzureVersion = "%home%/site/deployments/active";
            if(File.Exists(activeAzureVersion))
            {
                Version = File.ReadAllText(activeAzureVersion).FormatVersion();
            }
        }
    }
}
