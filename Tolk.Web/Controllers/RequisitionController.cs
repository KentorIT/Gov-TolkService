using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize]
    public class RequisitionController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly RequisitionService _requisitionService;
        private readonly EventLogService _eventLogService;
        private readonly ListToModelService _listToModelService;

        public RequisitionController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            ILogger<RequisitionController> logger,
            IOptions<TolkOptions> options,
            RequisitionService requisitionService,
            ListToModelService listToModelService,
            EventLogService eventLogService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _logger = logger;
            _options = options?.Value;
            _requisitionService = requisitionService;
            _listToModelService = listToModelService;
            _eventLogService = eventLogService;
        }

        public IActionResult List()
        {
            return View(new RequisitionListModel
            {
                FilterModel = new RequisitionFilterModel
                {
                    CustomerUnits = User.TryGetAllCustomerUnits(),
                    IsBroker = User.TryGetBrokerId().HasValue,
                    IsAdmin = User.IsInRole(Roles.SystemAdministrator) || User.IsInRole(Roles.ApplicationAdministrator)
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListRequisitions(IDataTablesRequest request)
        {
            var model = new RequisitionFilterModel();
            await TryUpdateModelAsync(model);
            model.IsCentralAdminOrOrderHandler = User.IsInRole(Roles.CentralAdministrator) || User.IsInRole(Roles.CentralOrderHandler);
            var brokerId = User.TryGetBrokerId();
            var customerOrganisationId = User.TryGetCustomerOrganisationId();
            model.IsBroker = brokerId.HasValue;
            if (model.IsBroker)
            {
                model.BrokerId = brokerId;
            }
            else
            {
                model.CustomerOrganisationId = customerOrganisationId;
                model.UserId = User.GetUserId();
                model.CustomerUnits = User.TryGetAllCustomerUnits();
            }

            IQueryable<Requisition> requisitions = null;

            if (customerOrganisationId.HasValue)
            {
                requisitions = model.GetRequisitionsFromOrders(_dbContext.Orders.Select(o => o));
            }
            else if (brokerId.HasValue)
            {
                requisitions = model.GetRequisitionsFromRequests(_dbContext.Requests.Select(o => o));
            }
            else
            {
                return Forbid();
            }
            return AjaxDataTableHelper.GetData(request, requisitions.Count(), model.Apply(requisitions), x => x.Select(r => new RequisitionListItemModel
            {
                OrderRequestId = customerOrganisationId.HasValue ? r.Request.OrderId : r.RequestId,
                Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name,
                OrderNumber = r.Request.Order.OrderNumber,
                OrderDateAndTime = $"{r.Request.CalculatedStartAt:yyyy-MM-dd HH:mm}-{r.Request.CalculatedEndAt:HH:mm}",
                Status = r.Status,
                BrokerName = r.Request.Ranking.Broker.Name,
                CustomerName = r.Request.Order.CustomerOrganisation.Name,
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<RequisitionListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(RequisitionListItemModel.CustomerName)).Visible = User.TryGetBrokerId().HasValue; 
            definition.Single(d => d.Name == nameof(RequisitionListItemModel.BrokerName)).Visible = User.TryGetCustomerOrganisationId().HasValue; 
            return Json(definition);
        }

        public async Task<IActionResult> View(int id, bool returnPartial = false)
        {
            var requisition = await _dbContext.Requisitions.GetFullRequisitionById(id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);

                model.UserCanAccept = (await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded;
                model.UserCanCreate = (await _authorizationService.AuthorizeAsync(User, requisition.Request, Policies.Edit)).Succeeded;
                model.RequestOrReplacingOrderPricesAreUsed = requisition.RequestOrReplacingOrderPeriodUsed;

                await _listToModelService.AddInformationFromListsToModel(model);

                model.EventLog = new EventLogModel
                {
                    Header = "Rekvisitionshändelser",
                    Id = "EventLog_Requisition",
                    DynamicLoadPath = $"Requisition/{nameof(GetEventLog)}/{id}",
                };
                if (returnPartial) { return PartialView(model); }
                return View(model);
            }
            return Forbid();
        }

        /// <summary>
        /// Create a requisition
        /// </summary>
        /// <param name="id">The Request to connect the requisition to</param>
        [Authorize(Policy = Policies.Broker)]
        public async Task<IActionResult> Create(int id)
        {
            var request = await _dbContext.Requests.GetRequestForOtherViewsById(id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
            {
                try
                {
                    var model = RequisitionModel.GetModelFromRequest(request);
                    Guid groupKey = Guid.NewGuid();

                    await _listToModelService.AddInformationFromListsToModel(model);
                    model.FileGroupKey = groupKey;
                    model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                    return View(model);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogError("Can't create requisition. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction("Index", "Home", new { errorMessage = $"Det gick inte att registrera rekvisition för {request.Order.OrderNumber}, det kan bero på att det redan finns en rekvisition registrerad." });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Broker)]
        public async Task<IActionResult> Create(RequisitionModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetRequestForRequisitionCreateById(model.RequestId);

                List<MealBreak> mealbreaks = new List<MealBreak>();
                if (model.MealBreaks != null)
                {
                    foreach (MealBreak mb in model.MealBreaks)
                    {
                        mealbreaks.Add(new MealBreak { StartAt = mb.StartAtTemp.ToDateTimeOffsetSweden(), EndAt = mb.EndAtTemp.ToDateTimeOffsetSweden() });
                    }
                }
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
                {
                    Requisition requisition;

                    try
                    {
                        requisition = await _requisitionService.Create(request, User.GetUserId(), User.TryGetImpersonatorId(), model.Message, model.Outlay,
                            model.SessionStartedAt, model.SessionEndedAt, model.TimeWasteTotalTime.HasValue ? (model.TimeWasteTotalTime ?? 0) - (model.TimeWasteIWHTime ?? 0) : model.TimeWasteTotalTime,
                            model.TimeWasteIWHTime, model.InterpreterTaxCard.Value, model.Files?.Select(f => new RequisitionAttachment { AttachmentId = f.Id }).ToList(), model.FileGroupKey.Value, mealbreaks, model.CarCompensation, model.PerDiem);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Failed to create requisition for request {request.RequestId}, message {errorMessage}.", request.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errorMessage = $"Det gick inte att registrera rekvisition för {request.Order.OrderNumber}, det kan bero på att det redan finns en rekvisition registrerad." });
                    }
                    return RedirectToAction("View", "Request", new { id = requisition.RequestId, tab = "requisition" });
                }
                return Forbid();
            }
            return View(nameof(Create), model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Review(int requisitionId)
        {
            var requisition = await _dbContext.Requisitions.GetRequisitionById(requisitionId);

            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    try
                    {
                        await _requisitionService.Review(requisition, User.GetUserId(), User.TryGetImpersonatorId());
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Failed to review requisition, requisitionId: {requisition.RequisitionId}, message {Message}", requisition.RequisitionId, ex.Message);
                        return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, errormessage = ex.Message });
                    }
                    return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmNoReview(int requisitionId)
        {
            var requisition = await _dbContext.Requisitions.GetRequisitionById(requisitionId);

            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    try
                    {
                        await _requisitionService.ConfirmNoReview(requisition, User.GetUserId(), User.TryGetImpersonatorId());
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("ConfirmNoReview failed for requisition, RequisitionId: {requisition.RequisitionId}, message {Message}", requisition.RequisitionId, ex.Message);
                        return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, errormessage = ex.Message });
                    }
                    return RedirectToAction("Index", "Home", new { Message = "Rekvisitionen är nu arkiverad" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Comment(CommentRequisitionModel model)
        {
            if (ModelState.IsValid)
            {
                var requisition = await _dbContext.Requisitions.GetRequisitionById(model.RequisitionId);

                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    try
                    {
                        await _requisitionService.Comment(requisition, User.GetUserId(), User.TryGetImpersonatorId(), model.CustomerComment);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Failed to comment requisition, RequisitionId: {requisition.RequisitionId}, message {Message}", requisition.RequisitionId, ex.Message);
                        return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, errormessage = ex.Message });
                    }
                    return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.RequisitionId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetEventLog(int id)
        {
            var requisition = await _dbContext.Requisitions.GetRequisitionById(id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = (await _eventLogService.GetEventLogForRequisitions(requisition.RequestId, requisition.Request.Order.CustomerOrganisation.Name, requisition.Request.Ranking.Broker.Name)).OrderBy(e => e.Timestamp)
                });
            }
            return Forbid();
        }
    }
}
