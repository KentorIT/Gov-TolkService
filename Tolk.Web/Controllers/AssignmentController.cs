﻿using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISwedishClock _clock;

        public AssignmentController(TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _clock = clock;
        }

        public IActionResult List(AssignmentFilterModel filterModel)
        {
            if (filterModel == null)
            {
                filterModel = new AssignmentFilterModel();
            }
            var requests = _dbContext.Requests.Include(r => r.Order)
                .Where(r => r.Status == RequestStatus.Approved || 
                r.Status == RequestStatus.CancelledByBroker || 
                r.Status == RequestStatus.CancelledByCreator ||
                r.Status == RequestStatus.CancelledByCreatorWhenApproved);
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.
            var interpreterId = User.TryGetInterpreterId();
            var brokerId = User.TryGetBrokerId();

            if (interpreterId.HasValue)
            {
                requests = requests.Where(r => r.InterpreterBrokerId == interpreterId);
            }
            if (brokerId.HasValue)
            {
                requests = requests.Where(r => r.Ranking.BrokerId == brokerId);
            }

            requests = filterModel.Apply(requests, _clock);

            return View(
               new AssignmentListModel
               {
                   FilterModel = filterModel,
                   Items = requests.SelectRequestListItemModel()
               });
        }

        public async Task<IActionResult> View(int id)
        {
            var request = _dbContext.Requests
                    .Include(r => r.PriceRows)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder)
                    .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(oa => oa.Attachment)
                    .Include(r => r.Attachments).ThenInclude(ra => ra.Attachment)
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
