using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Tolk.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public OrderController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public IActionResult Edit(int id)
        {
            //Get order model from db
            return View(OrderModel.Load(_dbContext, id, "x", 1));
        }

        public IActionResult Add()
        {
            return View("Edit");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Add(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                var order = model.Save(_dbContext, _userManager.GetUserId(User), 1);
                if (order != null)
                {
                    return Redirect($"~/Home/Index?message=Avropet%20har%20skickats. Sparades med Ordernummer: {order.OrderNumber}");
                }
            }
            return View("Edit", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Edit(OrderModel model)
        {
            return Add(model);
        }
    }
}
