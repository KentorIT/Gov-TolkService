using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Utilities;
using System.Collections.Generic;
using Tolk.Web.Helpers;
using System.Threading.Tasks;
using Tolk.Web.Authorization;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Customer)]
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly RankingService _rankingService;
        private readonly OrderService _orderService;

        public OrderController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            IAuthorizationService authorizationService,
            RankingService rankingService,
            OrderService orderService)
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _authorizationService = authorizationService;
            _rankingService = rankingService;
            _orderService = orderService;
        }

        public IActionResult List()
        {
            return View(_dbContext.Orders.Include(o => o.Language).Include(o => o.Region)
                .Where(r => r.CreatedBy == User.GetUserId())
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

        public IActionResult View(int id)
        {
            //Get order model from db
            var order = _dbContext.Orders
                .Include(o => o.CreatedByUser)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.Requirements)
                .ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests)
                .ThenInclude(r => r.Ranking)
                .ThenInclude(r => r.BrokerRegion)
                .ThenInclude(r => r.Broker)
                .Single(o => o.OrderId == id);
            var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(order.RequiredCompetenceLevel);
            var listType = order.CustomerOrganisation.PriceListType;
            //TODO: Handle this better. Preferably with a list that you can use contains on
            var request = order.Requests.SingleOrDefault(r =>
                r.Status == RequestStatus.Created ||
                r.Status == RequestStatus.Received ||
                r.Status == RequestStatus.Accepted ||
                r.Status == RequestStatus.SentToInterpreter ||
                r.Status == RequestStatus.Approved
                );
            var model = OrderModel.GetModelFromOrder(order, request?.RequestId);
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
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = _dbContext.Orders.Single(o => o.OrderId == id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                return View(OrderModel.GetModelFromOrder(order));
            }
            return Forbid();
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

                order = new Order
                {
                    //Hardcodes
                    RequiredInterpreterLocation = 1,
                    Status = OrderStatus.Requested,
                    CreatedBy = User.GetUserId(),
                    CreatedDate = DateTime.Now,
                    CustomerOrganisationId = User.GetCustomerOrganisationId(),
                    ImpersonatingCreator = User.GetImpersonatorId(),
                    Requirements = new List<OrderRequirement>()
                };

                model.UpdateOrder(order);
                _dbContext.Add(order);

                _orderService.CreateRequest(order);

                _dbContext.SaveChanges();

                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }
            return View("Edit", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                var order = _dbContext.Orders.Single(o => o.OrderId == model.OrderId);

                if (!(await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
                {
                    return Forbid();
                }

                model.UpdateOrder(order);

                _dbContext.SaveChanges();

                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }

            return View(model);
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
            request.AcceptanceBy = User.GetUserId();
            request.ImpersonatingAcceptanceBy = User.GetImpersonatorId();
            _dbContext.SaveChanges();
            return RedirectToAction(nameof(View), new { id = order.OrderId });
        }
    }
}
