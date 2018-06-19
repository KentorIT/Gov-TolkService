using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Tolk.Web.Helpers;
using Tolk.Web.Authorization;
using Tolk.BusinessLogic.Services;
using System.Threading.Tasks;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Broker)]
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly InterpreterService _interpreterService;

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService,
            InterpreterService interpreterService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
            _interpreterService = interpreterService;
        }

        public IActionResult List(RequestFilterModel model)
        {
            bool isCustomer = User.TryGetCustomerOrganisationId().HasValue;

            var items = _dbContext.Requests.Include(r => r.Order)
                        .Where(r => r.Ranking.BrokerRegion.Broker.BrokerId == User.GetBrokerId())
                        .Select(r => new RequestListItemModel
                        {
                            RequestId = r.RequestId,
                            Language = r.Order.Language.Name,
                            OrderNumber = r.Order.OrderNumber.ToString(),
                            CustomerName = r.Order.CustomerOrganisation.Name,
                            RegionName = r.Order.Region.Name,
                            Start = r.Order.StartAt,
                            End = r.Order.EndAt,
                            ExpiresAt = r.ExpiresAt,
                            Status = r.Status,
                            Action = ((!isCustomer && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)) || (isCustomer && r.Status == RequestStatus.Accepted) ? nameof(Process) : nameof(View))
                        });
            if (model.Status.HasValue)
            {
                items = items.Where(r => r.Status == model.Status);
            }

            return View(
                new RequestListModel
                {
                    Items = items,
                    FilterModel = model
                });
        }

        public async Task<IActionResult> View(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Ranking).ThenInclude(r => r.BrokerRegion).ThenInclude(b => b.Broker)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                //Get request model from db
                var model = RequestModel.GetModelFromRequest(request);
                model.BrokerId = request.Ranking.BrokerId;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Ranking).ThenInclude(r => r.BrokerRegion)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (request.Status == RequestStatus.Created)
                {
                    request.Received(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                }

                //Get request model from db
                var model = RequestModel.GetModelFromRequest(request);
                model.BrokerId = request.Ranking.BrokerId;
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Accept(RequestAcceptModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                    .Include(r => r.Order)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.Ranking)
                    .Single(o => o.RequestId == model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    int interpreterId = model.InterpreterId;
                    if (interpreterId == SelectListService.NewInterpreterId)
                    {
                        interpreterId = await _interpreterService.GetInterpreterId(
                            request.Ranking.BrokerId,
                            model.NewInterpreterEmail);
                    }

                    request.Accept(
                        _clock.SwedenNow,
                        User.GetUserId(),
                        User.TryGetImpersonatorId(),
                        interpreterId,
                        model.ExpectedTravelCosts,
                        model.InterpreterLocation,
                        model.CompetenceLevel,
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            RequestId = request.RequestId,
                            OrderRequirementId = ra.OrderRequirementId,
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement
                        }));

                    _dbContext.SaveChanges();

                    return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
                }
                return Forbid();
            }
            return View("Process", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Decline(RequestDeclineModel model)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order)
                .Include(r => r.Ranking)
                .Single(r => r.RequestId == model.RequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                //Get the order, and set Change the status on order and request?
                //TODO: Validate that the has the correct state, is connected to the user
                //Validate that the request is in correct state.
                //var order = _dbContext.Orders.Include(o => o.Requests)
                //    .ThenInclude(r => r.Ranking)
                //    .Single(o => o.OrderId == model.OrderId);
                //var request = order.Requests.Single(r => r.RequestId == model.RequestId);

                request.Order.Status = OrderStatus.Requested;

                request.Status = RequestStatus.DeclinedByBroker;
                request.AnswerDate = _clock.SwedenNow;
                request.AnsweredBy = User.GetUserId();
                request.ImpersonatingAnsweredBy = User.TryGetImpersonatorId();
                request.DenyMessage = model.DenyMessage;
                await _orderService.CreateRequest(request.Order);

                _dbContext.SaveChanges();
                return RedirectToAction(nameof(List));
            }

            return Forbid();
        }
    }
}
