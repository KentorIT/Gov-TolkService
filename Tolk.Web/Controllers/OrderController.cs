using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;

        public OrderController(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                var order = model.Save(_dbContext, "x", 1);
                if (order != null)
                {
                    return Redirect($"~/Home/Index?message=Avropet%20har%20skickats. Sparades med Ordernummer: {order.OrderNumber}");
                }
            }
            return View(model);
        }
    }
}
