﻿using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC method, cannot be static")]
        public ActionResult<string> Version()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        [HttpGet]
        public ActionResult<string> TestTime()
        {
            return _timeService.SwedenNow.ToSwedishString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
