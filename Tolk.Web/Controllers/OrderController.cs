using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Customer)]
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public OrderController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        #region possible moves to base controller

        protected string CurrentUserId
        {
            get
            {
                return _userManager.GetUserId(User);
            }
        }

        #endregion

        public IActionResult List()
        {
            //TODO:GET customer ID FROM CLAIMS
            return View(_dbContext.Orders.Include(o => o.Language).Include(o => o.Region)
                .Where(r => r.CreatedBy == CurrentUserId && r.CustomerOrganisationId == 1).Select(r => new OrderListItemModel
                {
                    OrderId = r.OrderId,
                    Language = r.Language.Name,
                    OrderNumber = r.OrderNumber.ToString(),
                    RegionName = r.Region.Name,
                    Start = r.StartDateTime,
                    End = r.EndDateTime,
                }));
        }


        public IActionResult Details(int id)
        {
            //Get order model from db
            return View(OrderModel.GetModelFromOrder(_dbContext.Orders
                .Include(o => o.CreatedByUser)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Single(o => o.OrderId == id)));
        }

        public IActionResult Edit(int id)
        {
            var order = _dbContext.Orders.Single(o => o.OrderId == id);
            //Get order model from db
            return View(OrderModel.GetModelFromOrder(order));
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
                Order order;
                if (model.OrderId.HasValue)
                {
                    order = _dbContext.Orders.Single(o => o.OrderId == model.OrderId);
                    //Modified by, Modified date, impersonator modifier
                }
                else
                {
                    order = new Order
                    {
                        //Hardcodes
                        RequiredInterpreterLocation = 1,
                        Status = OrderStatus.Requested,
                        CreatedBy = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        //Add as claim!!
                        CustomerOrganisationId = 1,
                        ImpersonatingCreator = User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)
                    };
                }

                order = model.UpdateOrder(order);
                if (!model.OrderId.HasValue)
                {
                    Order.CreateRequest(_dbContext, order);
                }
                _dbContext.SaveChanges();
                //TODO: If this is a edit, something else should happen to the requests in some way...
                return View("Details", OrderModel.GetModelFromOrder(order));
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
