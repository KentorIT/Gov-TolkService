﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.Api.Payloads;
using Tolk.BusinessLogic.Data;

namespace Tolk.Web.Api.Controllers
{
    public class HomeController : ControllerBase
    {
        public HomeController()
        {
        }

        [HttpGet]
        public ActionResult<string> Index()
        {

            return "Home/Index";
        }

        [HttpGet]
        public ActionResult<string> Ping()
        {
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;

            return clientCertInRequest?.SerialNumber;
        }
    }
}