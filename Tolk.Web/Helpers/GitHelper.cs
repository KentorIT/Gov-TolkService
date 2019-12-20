using System.IO;
using System.Reflection;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Helpers
{
    public static class GitHelper
    {
        public static string Version { get; } = "N/A";

        private static string FormatVersion(this string rawVersion)
        {
            return $"{rawVersion.Substring(0, 6)}";
        }

        static GitHelper()
        {
            var gitDir = "../.git";
            if (Directory.Exists(gitDir))
            {
                string versionNumber = File.ReadAllText($"../VersionNumber.txt");

                // Local, running in a repo directory.
                var head = File.ReadAllText($"{gitDir}/HEAD");
                string gitInfo;
                if (head.StartsWithSwedish("ref: "))
                {
                    var refFile = head.Substring(5).TrimEnd('\n');
                    gitInfo = File.ReadAllText($"{gitDir}/{refFile}").FormatVersion();
                }
                else
                {
                    gitInfo = head.FormatVersion();

                }
                Version = $"{versionNumber}.0-{gitInfo}";
            }
            else
            {
                //Get file version, if the site is run from artifacts built by build server.
                Version = $"{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version}";
            }

            var activeAzureVersion =
                System.Environment.ExpandEnvironmentVariables(
                "%HOME%\\site\\deployments\\active");

            if (File.Exists(activeAzureVersion))
            {
                Version = File.ReadAllText(activeAzureVersion).FormatVersion();
            }
        }
    }
}
