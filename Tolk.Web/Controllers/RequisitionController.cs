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
    [Authorize(Policy = Policies.Interpreter)]
    public class RequisitionController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;

        public RequisitionController(
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
            return View();
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
                return View(RequisitionModel.GetModelFromRequest(request));
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
                    request.CreateRequisition(new Requisition
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
                    });
                    _dbContext.SaveChanges();
                    return Redirect($"~/Home/Index?message=Rekvision har skapats");
                }
                return Forbid();
            }
            return View("Create", model);
        }
    }
}
