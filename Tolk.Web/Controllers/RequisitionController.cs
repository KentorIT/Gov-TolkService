using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
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

        public RequisitionController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            ILogger<RequisitionController> logger,
            IOptions<TolkOptions> options,
            RequisitionService requisitionService,
            EventLogService eventLogService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _logger = logger;
            _options = options?.Value;
            _requisitionService = requisitionService;
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
                OrderDateAndTime = $"{r.Request.Order.StartAt.ToSwedishString("yyyy-MM-dd")} {r.Request.Order.StartAt.ToSwedishString("HH\\:mm")}-{r.Request.Order.EndAt.ToSwedishString("HH\\:mm")}",
                Status = r.Status,
                BrokerName = r.Request.Ranking.Broker.Name,
                CustomerName = r.Request.Order.CustomerOrganisation.Name,
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<RequisitionListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(RequisitionListItemModel.CustomerName)).Visible = User.TryGetBrokerId().HasValue; //or is sys/app admin 
            definition.Single(d => d.Name == nameof(RequisitionListItemModel.BrokerName)).Visible = User.TryGetCustomerOrganisationId().HasValue; //or is sys/app admin 
            return Json(definition);
        }

        public async Task<IActionResult> View(int id)
        {
            var requisition = GetRequisition(id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
                var isAdmin = User.IsInRole(Roles.SystemAdministrator);
                var customerId = User.TryGetCustomerOrganisationId();
                model.AllowCreation = !isAdmin && !customerId.HasValue
                    && requisition.Request.Requisitions.All(r => r.Status == RequisitionStatus.Commented)
                    && requisition.Request.Requisitions.OrderBy(r => r.CreatedAt).Last().RequisitionId == requisition.RequisitionId;
                model.AllowProcessing = customerId.HasValue && requisition.ProcessAllowed && (await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded;
                model.AllowConfirmNoReview = customerId.HasValue && requisition.CofirmNoReviewAllowed && (await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded;
                model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(requisition.Request);
                model.RequestOrReplacingOrderPricesAreUsed = requisition.RequestOrReplacingOrderPeriodUsed;
                model.PreviousRequisitionView = GetPreviousRequisitionView(requisition.Request);
                model.RelatedRequisitions = requisition.Request.Requisitions
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => r.RequisitionId)
                    .ToList();
                model.EventLog = new EventLogModel
                {
                    Header = "Rekvisitionshändelser",
                    Id = "EventLog_Requisition",
                    DynamicLoadPath = $"Requisition/{nameof(GetEventLog)}/{id}",
                };
                return PartialView(model);
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
#warning include-fest
            var request = _dbContext.Requests
                .Include(r => r.Requisitions).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Requisitions).ThenInclude(r => r.PriceRows)
                .Include(r => r.Requisitions).ThenInclude(r => r.MealBreaks)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Interpreter)
                .Include(r => r.RequestViews).ThenInclude(rv => rv.ViewedByUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Include(r => r.PriceRows)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
            {
                if (!request.CanCreateRequisition)
                {
                    _logger.LogWarning("Wrong status when trying to Create requisition. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction("View", "Request", new { id, tab = "requisition" });
                }

                var model = RequisitionModel.GetModelFromRequest(request);
                Guid groupKey = Guid.NewGuid();

                //Get request model from db
                if (model.PreviousRequisition != null)
                {
#warning include-fest
                    var previousRequisition = _dbContext.Requisitions.Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .SingleOrDefault(r => r.RequisitionId == model.PreviousRequisition.RequisitionId);
                    // Get the attachments from the previous requisition.
                    List<FileModel> files = previousRequisition.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList();
                    model.Files = files.Any() ? files : null;
                    model.PreviousRequisition.ResultPriceInformationModel = GetRequisitionPriceInformation(previousRequisition, true);
                    model.SessionStartedAt = previousRequisition.SessionStartedAt;
                    model.SessionEndedAt = previousRequisition.SessionEndedAt;
                    model.MealBreaks = previousRequisition.MealBreaks.Any() ? previousRequisition.MealBreaks : null;
                }
                if (model.MealBreaks != null && model.MealBreaks.Any())
                {
                    foreach (MealBreak mb in model.MealBreaks)
                    {
                        mb.StartAtTemp = mb.StartAt.DateTime;
                        mb.EndAtTemp = mb.EndAt.DateTime;
                    }
                }
                model.FileGroupKey = groupKey;
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(request);
                model.Outlay = null;
                model.CarCompensation = null;
                model.PerDiem = null;
                if (request.RequestViews != null && request.RequestViews.Any(rv => rv.ViewedBy != User.GetUserId()))
                {
                    model.ViewedByUser = request.RequestViews.First(rv => rv.ViewedBy != User.GetUserId()).ViewedByUser.FullName + " håller också på med denna förfrågan";
                }
                return View(model);
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
                    catch (InvalidOperationException)
                    {
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
#warning include-fest
            var requisition = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Order)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.CreatedByUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Single(r => r.RequisitionId == requisitionId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    try
                    {
                        _requisitionService.Review(requisition, User.GetUserId(), User.TryGetImpersonatorId());
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning("{Message}, RequisitionId: {requisition.RequisitionId}", ex.Message, requisition.RequisitionId);
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
#warning include-fest
            var requisition = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.RequisitionStatusConfirmations)
                .Single(r => r.RequisitionId == requisitionId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    try
                    {
                        _requisitionService.ConfirmNoReview(requisition, User.GetUserId(), User.TryGetImpersonatorId());
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning("{Message}, RequisitionId: {requisition.RequisitionId}", ex.Message, requisition.RequisitionId);
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
#warning include-fest
                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .Single(r => r.RequisitionId == model.RequisitionId);
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    try
                    {
                        _requisitionService.Comment(requisition, User.GetUserId(), User.TryGetImpersonatorId(), model.CustomerComment);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning("{Message}, RequisitionId: {requisition.RequisitionId}", ex.Message, requisition.RequisitionId);
                        return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, errormessage = ex.Message });
                    }
                    return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.RequisitionId });
        }

        [HttpPost]
        public async Task<IActionResult> GetEventLog(int id)
        {
            var requisition = await _dbContext.Requisitions.GetRequisitionForEventLog(id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = (await _eventLogService.GetEventLogForRequisitions(requisition.RequestId, requisition.Request.Order.CustomerOrganisation.Name, requisition.Request.Ranking.Broker.Name)).OrderBy(e => e.Timestamp)
                });
            }
            return Forbid();
        }

        private Requisition GetRequisition(int id)
        {
#warning include-fest
            return _dbContext.Requisitions
                .Include(r => r.CreatedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.ProcessedUser)
                .Include(r => r.RequisitionStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(req => req.RequisitionStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(pr => pr.PriceRows)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(pr => pr.PriceRows).ThenInclude(plr => plr.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.ProcessedUser)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(req => req.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(req => req.MealBreaks)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.PriceRows).ThenInclude(r => r.PriceListRow)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Include(r => r.MealBreaks)
                .Single(o => o.RequisitionId == id);
        }

        private static PriceInformationModel GetRequisitionPriceInformation(Requisition requisition, bool useDisplayHideInfo = false)
        {
            if (requisition.PriceRows == null)
            {
                return null;
            }
            List<MealBreakInformation> mealBreakInformation = null;

            if (requisition.MealBreaks?.Count > 0)
            {
                mealBreakInformation = requisition.MealBreaks.Select(m => new MealBreakInformation
                {
                    StartAt = m.StartAt,
                    EndAt = m.EndAt
                }).ToList();
            }
            if (mealBreakInformation == null)
            {
                mealBreakInformation = new List<MealBreakInformation>();
            }

            PriceInformationModel pi = new PriceInformationModel
            {
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = useDisplayHideInfo ? "Pris enligt tidigare rekvisition" : string.Empty,
                UseDisplayHideInfo = useDisplayHideInfo,

            };
            pi.PriceInformationToDisplay.MealBreaks = mealBreakInformation;

            return pi;
        }

        private static PriceInformationModel GetRequisitionPriceInformation(Request request)
        {
            if (request.PriceRows == null)
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt bokningsbekräftelse",
                UseDisplayHideInfo = true,
                MealBreakIsNotDetucted = request.Order.MealBreakIncluded ?? false,
                Description = "Om rekvisitionen innehåller ersättning för bilersättning och traktamente kan förmedlingen komma att debitera påslag för sociala avgifter för de tolkar som inte är registrerade för F-skatt"
            };
        }

        private static RequisitionViewModel GetPreviousRequisitionView(Request request)
        {
            if (request.Requisitions == null || request.Requisitions.Count < 2)
            {
                return null;
            }
            var requisition = request.Requisitions
                .Where(r => r.Status == RequisitionStatus.Commented || r.Status == RequisitionStatus.DeniedByCustomer)
                .OrderByDescending(r => r.CreatedAt)
                .First();
            var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
            model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
            model.RequestPriceInformationModel = GetRequisitionPriceInformation(requisition.Request);
            model.RequestOrReplacingOrderPricesAreUsed = requisition.RequestOrReplacingOrderPeriodUsed;
            return model;
        }
    }
}
