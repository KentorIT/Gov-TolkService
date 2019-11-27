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
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public class OrderGroupController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly OrderService _orderService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly INotificationService _notificationService;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IMapper _mapper;

        public OrderGroupController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            IAuthorizationService authorizationService,
            OrderService orderService,
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
            _orderService = orderService;
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
            //, string message = null, string errorMessage = null Add these for message-handling later
            //Get order model from db
            OrderGroup orderGroup = await GetOrderGroup(id);

            var activeRequestGroup = orderGroup.ActiveRequestToBeProcessedForCustomer ?? orderGroup.RequestGroups.OrderBy(r => r.RequestGroupId).Last();

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                var model = OrderGroupModel.GetModelFromOrderGroup(orderGroup, activeRequestGroup);
                model.CustomerInformationModel = new CustomerInformationModel
                {
                    IsCustomer = true,
                    Name = model.CustomerName,
                    CreatedBy = model.CreatedBy,
                    OrganisationNumber = model.CustomerOrganisationNumber,
                    UnitName = model.CustomerUnitName,
                    DepartmentName = model.UnitName,
                    ReferenceNumber = model.CustomerReferenceNumber,
                    InvoiceReference = model.InvoiceReference
                };
                model.AllowProcessing = activeRequestGroup.Status == RequestStatus.Accepted && (await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Accept)).Succeeded;
                model.ActiveRequestGroup = RequestGroupViewModel.GetModelFromRequestGroupCustomer(activeRequestGroup);
                model.AllowOrderGroupCancellation = false;
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Approve(ProcessRequestGroupModel model)
        {
            var requestGroup = await _dbContext.RequestGroups
                .Include(r => r.Requests).ThenInclude(r => r.Order)
                .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestGroupId == model.RequestGroupId);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup.OrderGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.CanApprove)
                {
                    _logger.LogWarning("Wrong status when trying to Approve request group. Status: {requestGroup.Status}, RequestGroupId: {requestGroup.RequestGroupId}", requestGroup.Status, requestGroup.RequestGroupId);
                    return RedirectToAction(nameof(View), new { id = requestGroup.OrderGroupId });
                }
                _orderService.ApproveRequestGroupAnswer(requestGroup, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = requestGroup.OrderGroupId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Cancel(CancelOrderGroupModel model)
        {
            var orderGroup = await _dbContext.OrderGroups
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.CreatedByUser)
                .Include(o => o.Orders)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Ranking)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests)
                .SingleAsync(o => o.OrderGroupId == model.OrderGroupId );

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Cancel)).Succeeded)
            {
                if (orderGroup.CanCancel(_clock.SwedenNow))
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det går inte att avboka den sammanhållna bokningen." });
                }
                //_orderService.CancelOrderGroup(orderGroup, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                //await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { errorMessage = "Det funkar inte att avboka en sammanhållen bokning än" });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmNoAnswer(int orderGroupId)
        {
            var orderGroup = await _dbContext.OrderGroups.SingleAsync(o => o.OrderGroupId == orderGroupId);

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                if (orderGroup.Status != OrderStatus.NoBrokerAcceptedOrder)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Denna sammanhållna bokning var inte avböjd av samtliga förmedlingar." });
                }
                await _orderService.ConfirmNoAnswer(orderGroup, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Deny(ProcessRequestGroupModel model)
        {
            var requestGroup = await _dbContext.RequestGroups
                .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(r => r.Requests)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.OrderGroup).ThenInclude(o => o.Orders)
                .Include(r => r.OrderGroup).ThenInclude(o => o.RequestGroups)
                .SingleAsync(r => r.RequestGroupId == model.RequestGroupId);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup.OrderGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.CanDeny)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det går inte att neka denna tillsättning" });
                }
                await _orderService.DenyRequestGroupAnswer(requestGroup, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = requestGroup.OrderGroupId });
            }
            return Forbid();
        }

        private async Task<OrderGroup> GetOrderGroup(int id)
        {
            return await _dbContext.OrderGroups
                .Include(o => o.Language)
                .Include(o => o.CompetenceRequirements)
                .Include(o => o.Requirements)
                .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CreatedByUser)
                .Include(o => o.CustomerUnit)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(o => o.RequestGroups).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(o => o.RequestGroups).ThenInclude(r => r.AnsweringUser)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.AnsweringUser)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.ProcessingUser)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.CancelledByUser)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.Order)
                .Include(o => o.RequestGroups).ThenInclude(r => r.Requests).ThenInclude(r => r.Interpreter)
                .Include(o => o.Orders).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.Orders).ThenInclude(o => o.InterpreterLocations)
                .Include(o => o.Orders).ThenInclude(o => o.Requirements)
                .SingleAsync(o => o.OrderGroupId == id);
        }

    }
}
