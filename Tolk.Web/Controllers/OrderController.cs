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
using Tolk.BusinessLogic.Utilities;

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

        protected int CurrentCustomerOrgansationId
        {
            get
            {
                return int.Parse(User.Claims.Single(c => c.Type == TolkClaimTypes.CustomerOrganisationId).Value);
            }
        }

        #endregion

        public IActionResult List()
        {
            return View(_dbContext.Orders.Include(o => o.Language).Include(o => o.Region)
                .Where(r => r.CreatedBy == CurrentUserId && r.CustomerOrganisationId == CurrentCustomerOrgansationId).Select(r => new OrderListItemModel
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
            var order = _dbContext.Orders
                .Include(o => o.CreatedByUser)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.Requests)
                .ThenInclude(r => r.Ranking)
                .ThenInclude(r => r.BrokerRegion)
                .ThenInclude(r => r.Broker)
                .Single(o => o.OrderId == id);
            var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(order.RequiredCompetenceLevel);
            //TODO: Replace hardcoded pricelist type
            //TODO: Handle if there are no rows due to star/end date restrictions. Should take the one with the latest end date in that case.
            //TODO: Does not handle if the number of minutes is larger than 330!
            int maxMinutes = 330;
            //TODO: Has no calculation for övertid
            TimeSpan span = order.EndDateTime - order.StartDateTime;
            int totalMinutes = (int)span.TotalMinutes;
            int extraMinutes = 0;
            decimal extraTimePrice = 0;
            if (totalMinutes > maxMinutes)
            {
                extraMinutes = totalMinutes - maxMinutes;
                totalMinutes = maxMinutes;
            }
            var price = _dbContext.PriceListRows
                .OrderBy(r => r.MaxMinutes)
                .First(r =>
                    r.CompetenceLevel == competenceLevel &&
                    r.PriceListType == PriceListType.Other &&
                    r.PriceRowType == PriceRowType.BasePrice &&
                    r.StartDate <= order.StartDateTime.DateTime && r.EndDate >= order.StartDateTime.DateTime &&
                    r.MaxMinutes >= totalMinutes).Price;
            if (extraMinutes > 0)
            {
                var extraPriceInfo = _dbContext.PriceListRows
                    .OrderBy(r => r.MaxMinutes)
                    .Single(r =>
                        r.CompetenceLevel == competenceLevel &&
                        r.PriceListType == PriceListType.Other &&
                        r.PriceRowType == PriceRowType.PriceOverMaxTime &&
                        r.StartDate <= order.StartDateTime.DateTime && r.EndDate >= order.StartDateTime.DateTime);
                int n = extraMinutes / extraPriceInfo.MaxMinutes;
                extraTimePrice = n * extraPriceInfo.Price;
            }
            var model = OrderModel.GetModelFromOrder(order);
            //TODO: Handle this better.
            var rank = order.Requests.Single(r =>
                r.Status == RequestStatus.Created ||
                r.Status == RequestStatus.Received ||
                r.Status == RequestStatus.Accepted ||
                r.Status == RequestStatus.SentToInterpreter ||
                r.Status == RequestStatus.Approved
                ).Ranking;
            model.CalculatedPrice = (price + extraTimePrice) * rank.BrokerFee;
            model.BrokerName = rank.BrokerRegion.Broker.Name;
            return View("Details", model);
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
                        CustomerOrganisationId = CurrentCustomerOrgansationId,
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
                return Details(order.OrderId);
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
