using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                return Redirect("~/Home/Index?message=Avropet%20har%20skickats");
            }
            else
            {
                return Redirect("~/Home/Index?message=Avropet%20har%20INTE%20skickats");
            }
        }
    }
}
