using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System.ComponentModel;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC method, cannot be static")]
        public ActionResult<string> Index()
        {
            return string.Empty;
        }

        [HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC method, cannot be static")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC method, cannot be static")]
        public ActionResult<string> Version()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        [HttpGet]
        [OpenApiIgnore]
        public ActionResult<string> TestTime()
        {
            return _timeService.SwedenNow.ToSwedishString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
