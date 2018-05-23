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
using System.Collections.Generic;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Customer)]
    public class OrderController : BaseController
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly PriceCalculationService _priceCalculationService;

        public OrderController(TolkDbContext dbContext, UserManager<AspNetUser> userManager, PriceCalculationService priceCalculationService)
           : base(userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _priceCalculationService = priceCalculationService;
        }

        private int CurrentCustomerOrgansationId
        {
            get
            {
                return int.Parse(User.Claims.Single(c => c.Type == TolkClaimTypes.CustomerOrganisationId).Value);
            }
        }

        public IActionResult List()
        {
            return View(_dbContext.Orders.Include(o => o.Language).Include(o => o.Region)
                .Where(r => r.CreatedBy == CurrentUserId && r.CustomerOrganisationId == CurrentCustomerOrgansationId)
                .Select(o => new OrderListItemModel
                {
                    OrderId = o.OrderId,
                    Language = o.Language.Name,
                    OrderNumber = o.OrderNumber.ToString(),
                    RegionName = o.Region.Name,
                    Start = o.StartDateTime,
                    End = o.EndDateTime,
                    Status = o.Status
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
            var listType = order.CustomerOrganisation.PriceListType;
            var model = OrderModel.GetModelFromOrder(order);
            //TODO: Handle this better. Preferably with a list that you can use contains on
            var request = order.Requests.SingleOrDefault(r =>
                r.Status == RequestStatus.Created ||
                r.Status == RequestStatus.Received ||
                r.Status == RequestStatus.Accepted ||
                r.Status == RequestStatus.SentToInterpreter ||
                r.Status == RequestStatus.Approved
                );
            model.CalculatedPrice = _priceCalculationService.GetPrices(order.StartDateTime, order.EndDateTime, competenceLevel, listType, (request?.Ranking.BrokerFee ?? 0));
            model.RequestStatus = request?.Status;
            model.BrokerName = request?.Ranking.BrokerRegion.Broker.Name;
            if (request != null && (request.Status == RequestStatus.Accepted || request.Status == RequestStatus.Approved))
            {
                model.RequestId = request.RequestId;
                model.ExpectedTravelCosts = request.ExpectedTravelCosts ?? 0;
                model.InterpreterName = _dbContext.Requests
                    .Include(r => r.Interpreter)
                    .ThenInclude(i => i.User)
                    .Single(r => r.RequestId == request.RequestId).Interpreter?.User.NormalizedEmail;
            }
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
                        CreatedBy = int.Parse(_userManager.GetUserId(User)),
                        CreatedDate = DateTime.Now,
                        CustomerOrganisationId = CurrentCustomerOrgansationId,
                        ImpersonatingCreator = CurrentImpersonatorId
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

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Approve(ApproveModel model)
        {
            //Get the order, and set Change the status on order and request, and also modified?
            //TODO: Validate that the has the correct state, is conneted to the user
            //Validate that the request is in correct state.
            var order = _dbContext.Orders.Include(o => o.Requests).Single(o => o.OrderId == model.OrderId);
            var request = order.Requests.Single(r => r.RequestId == model.RequestId);
            order.Status = OrderStatus.ResponseAccepted;
            request.Status = RequestStatus.Approved;
            request.AcceptanceDate = DateTimeOffset.Now;
            request.AcceptanceBy = CurrentUserId;
            request.ImpersonatingAcceptanceBy = CurrentImpersonatorId;
            _dbContext.SaveChanges();
            return Details(order.OrderId);
        }
    }
}
