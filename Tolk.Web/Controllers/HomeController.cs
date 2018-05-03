using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string message)
        {
            return View(new StartViewModel { Message = message });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
