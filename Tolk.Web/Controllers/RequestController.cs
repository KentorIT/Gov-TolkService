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

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock, 
            OrderService orderService,
            IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
        }

        public IActionResult List()
        {
            return View(_dbContext.Requests.Include(r => r.Order)
                .Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) 
                && r.Ranking.BrokerRegion.Broker.BrokerId == User.GetBrokerId())
                .Select(r => new RequestListItemModel
                {
                    RequestId = r.RequestId,
                    Language = r.Order.Language.Name,
                    OrderNumber = r.Order.OrderNumber.ToString(),
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    RegionName = r.Order.Region.Name,
                    Start = r.Order.StartDateTime,
                    End = r.Order.EndDateTime,
                    Status = r.Status
                }));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Ranking)
                .Single(o => o.RequestId == id);

            if((await _authorizationService.AuthorizeAsync(User, request, Policies.Edit)).Succeeded)
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
        public IActionResult Edit(RequestModel model)
        {
            if (ModelState.IsValid)
            {
                //TODO: VERIFY THAT THE REQUEST IS CONNECTED TO THE USER'S BROKER!!
                var request = _dbContext.Requests
                    .Include(r => r.Order)
                    .Include(r => r.RequirementAnswers)
                    .Single(o => o.RequestId == model.RequestId);
                request.Status = model.SetStatus;
                request.AnswerDate = _clock.SwedenNow;
                request.AnsweredBy = User.GetUserId();
                request.ImpersonatingAnsweredBy = User.TryGetImpersonatorId();

                request.InterpreterId = model.InterpreterId;
                request.ExpectedTravelCosts = model.ExpectedTravelCosts;
                request.InterpreterLocation = (int?)model.InterpreterLocation;
                request.CompetenceLevel = (int?)model.CompetenceLevel;
                if (model.RequirementAnswers != null)
                {
                    // answer all extra requirements
                    foreach (var answer in model.RequirementAnswers)
                    {
                        request.RequirementAnswers.Add(
                            new OrderRequirementRequestAnswer
                            {
                                RequestId = request.RequestId,
                                OrderRequirementId = answer.OrderRequirementId,
                                Answer = answer.Answer,
                                CanSatisfyRequirement = answer.CanMeetRequirement,
                            });
                    }
                }

                //TODO: This should differ depending on the incoming status.
                request.Order.Status = OrderStatus.RequestResponded;

                _dbContext.SaveChanges();

                return Redirect($"~/Home/Index?message=Svar har skickats");
            }
            return View("Edit", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Decline(ProcessRequestModel model)
        {
            //Get the order, and set Change the status on order and request?
            //TODO: Validate that the has the correct state, is connected to the user
            //Validate that the request is in correct state.
            var order = _dbContext.Orders.Include(o => o.Requests)
                .ThenInclude(r => r.Ranking)
                .Single(o => o.OrderId == model.OrderId);
            var request = order.Requests.Single(r => r.RequestId == model.RequestId);
            order.Status = OrderStatus.Requested;

            request.Status = RequestStatus.DeclinedByBroker;
            request.AnswerDate = DateTimeOffset.Now;
            request.AnsweredBy = User.GetUserId();
            request.ImpersonatingAnsweredBy = User.TryGetImpersonatorId();
            request.DenyMessage = model.DenyMessage;
            _orderService.CreateRequest(order);

            _dbContext.SaveChanges();
            return RedirectToAction(nameof(List));
        }
    }
}
