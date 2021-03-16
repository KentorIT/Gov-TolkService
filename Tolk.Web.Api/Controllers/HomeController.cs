using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class HomeController : ControllerBase
    {
        private readonly ISwedishClock _timeService;
        private readonly CacheService _cacheService;

        public HomeController(ISwedishClock timeService, CacheService cacheService)
        {
            _timeService = timeService;
            _cacheService = cacheService;
        }

        [HttpGet]
        [OpenApiIgnore]
        public ActionResult<string> Index()
        {
            return string.Empty;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(string))]
        [Description("Returnerar en specifik sträng (Pong)")]
        [OpenApiTag("Home", AddToDocument = true, Description = "Enkla basanrop för verifiering")]
        public ActionResult<string> Ping()
        {
            return "Pong";
        }

        [HttpGet]
        [OpenApiIgnore]
        public ActionResult<string> TestCertificate()
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            return clientCertInRequest?.SerialNumber;
        }

        [HttpGet]
        [OpenApiIgnore]
        public async Task<ActionResult<string>> FlushCaches()
        {
            await _cacheService.FlushAll();
            //Since it only is three caches right now, this does not have to be authentication-hidden.
            return "Cacherna har rensats.";
        }


        [HttpGet]
        [ProducesResponseType(200, Type = typeof(string))]
        [Description("Returnerar en sträng med nuvarande version på systemet")]
        [OpenApiTag("Home")]
        public ActionResult<string> Version()
        {
            var gitDir = "../.git";
            if (Directory.Exists(gitDir) && System.IO.File.Exists($"{gitDir}/HEAD"))
            {
                string versionNumber = System.IO.File.ReadAllText($"../VersionNumber.txt");

                // Local, running in a repo directory.
                var head = System.IO.File.ReadAllText($"{gitDir}/HEAD");
                string gitInfo;
                if (head.StartsWithSwedish("ref: "))
                {
                    var refFile = head[5..].TrimEnd('\n');
                    if (System.IO.File.Exists($"{gitDir}/{refFile}"))
                    {
                        gitInfo = FormatVersion(System.IO.File.ReadAllText($"{gitDir}/{refFile}"));
                    }
                    else
                    {
                        gitInfo = FormatVersion(head);
                    }
                }
                else
                {
                    gitInfo = FormatVersion(head);

                }
                return $"{versionNumber}.0-{gitInfo}";
            }

            var activeAzureVersion =
                System.Environment.ExpandEnvironmentVariables(
                "%HOME%\\site\\deployments\\active");

            if (System.IO.File.Exists(activeAzureVersion))
            {
                return FormatVersion(System.IO.File.ReadAllText(activeAzureVersion));
            }
            //Get file version, if the site is run from artifacts built by build server.
            return $"{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version}";
        }

        [HttpGet]
        [OpenApiIgnore]
        public ActionResult<string> TestTime()
        {
            return _timeService.SwedenNow.ToSwedishString("yyyy-MM-dd HH:mm:ss");
        }

        private static string FormatVersion(string rawVersion)
        {
            return $"{rawVersion.Substring(0, 6)}";
        }

    }
}
