using AutoMapper;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Broker)]
    public class RequestGroupController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly RequestService _requestService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly INotificationService _notificationService;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IMapper _mapper;

        public RequestGroupController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            IAuthorizationService authorizationService,
            RequestService requestService,
            DateCalculationService dateCalculationService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            IOptions<TolkOptions> options,
            INotificationService notificationService,
            UserManager<AspNetUser> usermanager,
            IMapper mapper
            )
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _authorizationService = authorizationService;
            _requestService = requestService;
            _dateCalculationService = dateCalculationService;
            _clock = clock;
            _logger = logger;
            _options = options.Value;
            _notificationService = notificationService;
            _userManager = usermanager;
            _mapper = mapper;
        }


        public async Task<IActionResult> View(int id)
        {
            var requestGroup = await _dbContext.RequestGroups
                .SingleAsync(r => r.RequestGroupId == id);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.View)).Succeeded)
            {
                if (requestGroup.IsToBeProcessedByBroker)
                {
                    return RedirectToAction(nameof(Process), new { id = requestGroup.RequestGroupId });
                }
                return View(RequestGroupViewModel.GetModelFromRequestGroup(requestGroup));
            }
            return Forbid();

        }

        public async Task<IActionResult> Process(int id)
        {
            var requestGroup = await _dbContext.RequestGroups
                .SingleAsync(r => r.RequestGroupId == id);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process request group. Status: {request.Status}, RequestId: {request.RequestGroupId}", requestGroup.Status, requestGroup.RequestGroupId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (requestGroup.Status == RequestStatus.Created)
                {
                    _requestService.AcknowledgeGroup(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }

                RequestGroupProcessModel model = RequestGroupProcessModel.GetModelFromRequestGroup(requestGroup, new Guid(), _options.CombinedMaxSizeAttachments);
                return View(model);
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(RequestGroupProcessModel model)
        {
            if (ModelState.IsValid)
            {
            }
            await _dbContext.SaveChangesAsync();

            return View(nameof(Process), model);
        }
    }
}
