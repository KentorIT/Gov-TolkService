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
                Order order = new Order
                {
                    //Hardcodes
                    CreatedBy = "x",
                    CustomerOrganisationId = 1,
                    RequiredInterpreterLocation = 1,
                    Status = 1,
                    //end hardcodes
                    CreatedDate = DateTime.Now,
                    LanguageId = model.Language,
                    AllowMoreThanTwoHoursTravelTime = model.AllowMoreThanTwoHoursTravelTime,
                    AssignentType = model.AssignentType,
                    RegionId = model.RegionId,
                    CustomerReferenceNumber = model.CustomerReferenceNumber,
                    StartDateTime = model.StartDateTime,
                    EndDateTime = model.EndDateTime,
                    Description = model.Description,
                    UnitName = model.UnitName,
                    Street = model.LocationStreet,
                    ZipCode = model.LocationZipCode,
                    City = model.LocationCity,
                    RequiredCompetenceLevel = model.RequiredCompetenceLevel,
                };
                _dbContext.Orders.Add(order);
                _dbContext.SaveChanges();
                return Redirect($"~/Home/Index?message=Avropet%20har%20skickats. Sparades med Ordernummer: {order.OrderNumber}");
            }
            else
            {
                return Redirect("~/Home/Index?message=Avropet%20har%20INTE%20skickats");
            }
        }
    }
}
