using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ISwedishClock _timeService;

        public HomeController(ISwedishClock timeService)
        {
            _timeService = timeService;
        }

        [HttpGet(nameof(Index))]
        public ActionResult<string> Index()
        {
            return string.Empty;
        }

        [HttpGet(nameof(Ping))]
        public ActionResult<string> Ping()
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;

            return clientCertInRequest?.SerialNumber;
        }

        [HttpGet(nameof(Version))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC method, cannot be static")]
        public ActionResult<string> Version()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        [HttpGet(nameof(TestTime))]
        public ActionResult<string> TestTime()
        {
            return _timeService.SwedenNow.ToSwedishString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
