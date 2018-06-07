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
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Controllers
{
    public class RequisitionController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly PriceCalculationService _priceCalculationService;

        public RequisitionController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> View(int id)
        {
            var requisition = _dbContext.Requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(o => o.BrokerRegion).ThenInclude(o => o.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(o => o.BrokerRegion).ThenInclude(o => o.Region)
              .Single(o => o.RequisitionId == id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel.Value);
                var request = requisition.Request;
                var order = request.Order;
                var listType = order.CustomerOrganisation.PriceListType;
                var model = RequisitionViewModel.GetViewModelFromrequisition(requisition);
                model.CalculatedPrice = _priceCalculationService.GetPrices(order.StartDateTime, order.EndDateTime, competenceLevel, listType, request.Ranking.BrokerFee);
                model.ResultingPrice = _priceCalculationService.GetPrices(requisition.SessionStartedAt, requisition.SessionEndedAt, competenceLevel, listType, (request.Ranking.BrokerFee), 
                    requisition.TimeWasteBeforeStartedAt, requisition.TimeWasteAfterEndedAt);
                return View(model);
            }
            return Forbid();
        }

        /// <summary>
        /// Create a requisition
        /// </summary>
        /// <param name="id">The Request to connect the requisition to</param>
        /// <returns></returns>
        public async Task<IActionResult> Create(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking).ThenInclude(o => o.BrokerRegion).ThenInclude(o => o.Broker)
                .Include(r => r.Ranking).ThenInclude(o => o.BrokerRegion).ThenInclude(o => o.Region)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
            {
                //Get request model from db
                return View(RequisitionViewModel.GetModelFromRequest(request));
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(RequisitionModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                    .Include(r => r.Order)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Ranking).ThenInclude(o => o.BrokerRegion)
                    .Single(o => o.RequestId == model.RequestId);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
                {
                    var requisition = new Requisition
                    {
                        Status = RequisitionStatus.Created,
                        CreatedBy = User.GetUserId(),
                        CreatedAt = _clock.SwedenNow.DateTime,
                        ImpersonatingCreatedBy = User.TryGetImpersonatorId(),
                        TravelCosts = model.TravelCosts,
                        Message = model.Message,
                        SessionStartedAt = model.SessionStartedAt,
                        SessionEndedAt = model.SessionEndedAt,
                        TimeWasteBeforeStartedAt = model.TimeWasteBeforeStartedAt,
                        TimeWasteAfterEndedAt = model.TimeWasteAfterEndedAt,
                    };
                    request.CreateRequisition(requisition);
                    _dbContext.SaveChanges();
                    return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
                }
                return Forbid();
            }
            return View("Create", model);
        }
    }
}
