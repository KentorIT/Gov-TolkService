using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tolk.Api.Payloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Api.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISwedishClock _timeService;

        public HomeController(ISwedishClock timeService)
        {
            _timeService = timeService;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult<string> Ping()
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;

            return clientCertInRequest?.SerialNumber;
        }
        [HttpGet]
        public ActionResult<string> TestTime()
        {
            return _timeService.SwedenNow.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
