using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public class OrderGroupController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly OrderService _orderService;
        private readonly RequestService _requestService;
        private readonly CacheService _cacheService;
        private readonly ListToModelService _listToModelService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;

        public OrderGroupController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            OrderService orderService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            CacheService cacheService,
            ListToModelService listToModelService,
            RequestService requestService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _orderService = orderService;
            _clock = clock;
            _logger = logger;
            _cacheService = cacheService;
            _listToModelService = listToModelService;
            _requestService = requestService;
        }

        public async Task<IActionResult> View(int id)
        {
            OrderGroup orderGroup = await _dbContext.OrderGroups.GetFullOrderGroupById(id);

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                var allowEdit = (await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Edit)).Succeeded;

                var activeRequestGroup = await _dbContext.RequestGroups.GetLastRequestGroupForOrderGroup(id);
                await _orderService.AddOrdersWithListsForGroup(orderGroup);
                await _requestService.AddListsForRequestGroup(activeRequestGroup);
                activeRequestGroup.Requests.ForEach(r => r.Order = orderGroup.Orders.Where(o => o.OrderId == r.OrderId).SingleOrDefault());
                
                var model = OrderGroupModel.GetModelFromOrderGroup(orderGroup, activeRequestGroup);
                await _listToModelService.AddInformationFromListsToModel(model);

                model.CustomerInformationModel = new CustomerInformationModel
                {
                    IsCustomer = true,
                    Name = model.CustomerName,
                    CreatedBy = model.CreatedBy,
                    OrganisationNumber = model.CustomerOrganisationNumber,
                    PeppolId = model.CustomerPeppolId,
                    UnitName = model.CustomerUnitName,
                    DepartmentName = model.UnitName,
                    ReferenceNumber = model.CustomerReferenceNumber,
                    InvoiceReference = model.InvoiceReference,
                    UseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == orderGroup.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter))
                };

                model.ActiveRequestGroup = RequestGroupViewModel.GetModelFromRequestGroup(activeRequestGroup, User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator));

                await _listToModelService.AddInformationFromListsToModel(model.ActiveRequestGroup);
                model.AllowProcessing = activeRequestGroup.Status == RequestStatus.AnsweredAwaitingApproval && (await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Accept)).Succeeded;
                model.AllowCancellation = orderGroup.AllowCancellation && (await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Cancel)).Succeeded;
                model.AllowNoAnswerConfirmation = orderGroup.AllowNoAnswerConfirmation && allowEdit;
                model.AllowResponseNotAnsweredConfirmation = orderGroup.AllowResponseNotAnsweredConfirmation && allowEdit;
                model.AllowUpdateExpiry = orderGroup.AllowUpdateExpiry && allowEdit;
                model.UseAttachments = CachedUseAttachentSetting(orderGroup.CustomerOrganisationId);
                SetCustomerSpecificViewProperties(model.CustomerInformationModel);
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Approve(ProcessRequestGroupModel model)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupById(model.RequestGroupId);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup.OrderGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.CanApprove)
                {
                    _logger.LogError("Wrong status when trying to Approve request group. Status: {requestGroup.Status}, RequestGroupId: {requestGroup.RequestGroupId}", requestGroup.Status, requestGroup.RequestGroupId);
                    return RedirectToAction(nameof(View), new { id = requestGroup.OrderGroupId });
                }
                await _orderService.ApproveRequestGroupAnswer(requestGroup, User.GetUserId(), User.TryGetImpersonatorId());
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
            var orderGroup = await _dbContext.OrderGroups.GetOrderGroupById(model.OrderGroupId);

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Cancel)).Succeeded)
            {
                if (!orderGroup.AllowCancellation)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det går inte att avboka den sammanhållna bokningen." });
                }
                try
                {
                    await _orderService.CancelOrderGroup(orderGroup, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Index", "Home", new { Message = "Den sammanhållna bokningen är nu avbokad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("Cancel failed for ordergroup, OrderGroupId: {model.OrderGroupId}, message {ex.Message}", model.OrderGroupId, ex.Message);
                    return RedirectToAction("Index", "Home", new { ErrorMessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmNoAnswer(int orderGroupId)
        {
            var orderGroup = await _dbContext.OrderGroups.GetOrderGroupById(orderGroupId);
            if (orderGroup.Status == OrderStatus.NoBrokerAcceptedOrder && (await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                try
                {
                    await _orderService.ConfirmGroupNoAnswer(orderGroup, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Sammanhållen bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmNoAnswer failed for ordergroup, OrderGroupId: {orderGroupId}, message {ex.Message}", orderGroupId, ex.Message);
                    return RedirectToAction("Index", "Home", new { ErrorMessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmResponseNotAnswered(int orderGroupId)
        {
            var orderGroup = await _dbContext.OrderGroups.GetOrderGroupById(orderGroupId);
            if (orderGroup.Status == OrderStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                try
                {
                    await _orderService.ConfirmGroupResponeNotAnswered(orderGroup, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Sammanhållen bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmResponseNotAnswered failed for ordergroup, OrderGroupId: {orderGroupId}, message {ex.Message}", orderGroupId, ex.Message);
                    return RedirectToAction("Index", "Home", new { ErrorMessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Deny(ProcessRequestGroupModel model)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupById(model.RequestGroupId);

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

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> UpdateExpiry(int orderGroupId, DateTimeOffset latestAnswerBy)
        {
            var orderGroup = await _dbContext.OrderGroups.GetOrderGroupById(orderGroupId);

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.Edit)).Succeeded)
            {
                var requestGroup = await _dbContext.RequestGroups.GetRequestGroupsForOrderGroup(orderGroupId).SingleOrDefaultAsync(r => r.Status == RequestStatus.AwaitingDeadlineFromCustomer);
                if (requestGroup == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Denna sammanhållna bokning behöver inte få sista svarstid satt." });
                }

                await _orderService.SetRequestGroupExpiryManually(requestGroup, latestAnswerBy, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = $"Sista svarstid för sammanhållen bokning {orderGroup.OrderGroupNumber} är satt" });
            }
            return Forbid();
        }

        private bool CachedUseAttachentSetting(int customerOrganisationId) => _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && !c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.HideAttachmentField));

        private CustomerInformationModel SetCustomerSpecificViewProperties(CustomerInformationModel model)
        {
            var customerSpecificProperties = _cacheService.ActiveCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.TryGetCustomerOrganisationId()).ToList();
            foreach (var property in customerSpecificProperties)
            {
                switch (property.PropertyToReplace)
                {
                    case PropertyType.InvoiceReference:
                        var customerSpecific = property;
                        customerSpecific.Value = model.InvoiceReference;
                        model.CustomerSpecificInvoiceReference = customerSpecific;
                        break;
                    default:
                        break;
                }
            }
            return model;
        }
    }
}
