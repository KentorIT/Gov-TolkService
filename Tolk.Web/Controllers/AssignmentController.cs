﻿using Microsoft.AspNetCore.Mvc;
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
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;


namespace Tolk.Web.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISwedishClock _clock;

        public AssignmentController(TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            IAuthorizationService authorizationService,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _clock = clock;
        }

        public IActionResult List(AssignmentFilterModel filterModel)
        {
            var requests = _dbContext.Requests.Include(r => r.Order)
                .Where(r => r.Status == RequestStatus.Approved || 
                r.Status == RequestStatus.CancelledByBroker || 
                r.Status == RequestStatus.CancelledByBrokerConfirmed ||
                r.Status == RequestStatus.CancelledByCreator ||
                r.Status == RequestStatus.CancelledByCreatorConfirmed ||
                r.Status == RequestStatus.CancelledByBrokerConfirmed
                );
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.
            var interpreterId = User.TryGetInterpreterId();
            var brokerId = User.TryGetBrokerId();

            if (interpreterId.HasValue)
            {
                requests = requests.Where(r => r.InterpreterId == interpreterId);
            }
            if (brokerId.HasValue)
            {
                requests = requests.Where(r => r.Ranking.BrokerId == brokerId);
            }
            // Filters
            if (filterModel != null)
            {
                requests = filterModel.Apply(requests, _clock);
            }

            return View(
               new AssignmentListModel
               {
                   FilterModel = filterModel,
                   Items = requests.Select(r => new RequestListItemModel
                   {
                       RequestId = r.RequestId,
                       Language = r.Order.OtherLanguage ?? r.Order.Language.Name ?? "(Tolkanvändarutbildning)",
                       OrderNumber = r.Order.OrderNumber,
                       CustomerName = r.Order.CustomerOrganisation.Name,
                       RegionName = r.Order.Region.Name,
                       Start = r.Order.StartAt,
                       End = r.Order.EndAt,
                       Status = r.Status,
                       Action = nameof(View)
                   })
               });
        }

        public async Task<IActionResult> View(int id)
        {
            var request = _dbContext.Requests
                    .Include(r => r.Requisitions)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Where(r => r.RequestId == id).Single();
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return View(AssignmentModel.GetModelFromRequest(request, _clock.SwedenNow));
            }
            return Forbid();
        }
    }
}
