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

namespace Tolk.Web.Controllers
{
    public class RequisitionController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly NotificationService _notificationService;

        public RequisitionController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService,
            ILogger<RequisitionController> logger,
            IOptions<TolkOptions> options,
            NotificationService notificationService
            )
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
            _logger = logger;
            _options = options.Value;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> View(int id, bool returnPartial = false)
        {
            var requisition = _dbContext.Requisitions
                .Include(r => r.CreatedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.ProcessedUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(pr => pr.PriceRows)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.CreatedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).Include(r => r.ProcessedUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.PriceRows).ThenInclude(r => r.PriceListRow)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
              .Single(o => o.RequisitionId == id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
                var customerId = User.TryGetCustomerOrganisationId();
                model.AllowCreation = !customerId.HasValue && requisition.Request.Requisitions.All(r => r.Status == RequisitionStatus.DeniedByCustomer);
                model.AllowProcessing = customerId.HasValue && requisition.Status == RequisitionStatus.Created && (await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded;
                model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(requisition.Request);
                model.RequestOrReplacingOrderPricesAreUsed = requisition.RequestOrReplacingOrderPeriodUsed;
                model.EventLog = new EventLogModel
                {
                    Entries = EventLogHelper.GetEventLog(requisition.Request.Requisitions, requisition.Request.Order.CustomerOrganisation.Name)
                        .OrderBy(e => e.Timestamp).ToList()
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
        public async Task<IActionResult> Create(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Requisitions).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Requisitions).ThenInclude(r => r.PriceRows)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Include(r => r.PriceRows)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
            {
                var model = RequisitionModel.GetModelFromRequest(request);
                Guid groupKey = Guid.NewGuid();

                //Get request model from db
                if (model.PreviousRequisition != null)
                {
                    var previousRequisition = _dbContext.Requisitions.Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .SingleOrDefault(r => r.RequisitionId == model.PreviousRequisition.RequisitionId);
                    // Get the attachments from the previous requisition.
                    List<FileModel> files = previousRequisition.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList();
                    model.Files = files.Count() > 0 ? files : null;
                    model.PreviousRequisition.ResultPriceInformationModel = GetRequisitionPriceInformation(previousRequisition, true);
                }
                model.FileGroupKey = groupKey;
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(request);
                return View(model);
            }
            return Forbid();
        }

        public IActionResult List(RequisitionFilterModel model)
        {
            if (model == null)
            {
                model = new RequisitionFilterModel();
            }

            var brokerId = User.TryGetBrokerId();
            var customerId = User.TryGetCustomerOrganisationId();
            model.IsCustomer = customerId.HasValue;
            model.IsBroker = brokerId.HasValue;

            var requisitions = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Where(r => !r.ReplacedByRequisitionId.HasValue);
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.
            var userId = User.GetUserId();

            if (customerId.HasValue)
            {
                if (User.IsInRole(Roles.SuperUser))
                {
                    requisitions = requisitions.Where(r => r.Request.Order.CustomerOrganisationId == customerId);
                }
                else
                {
                    if (!model.FilterByContact.HasValue)
                    {
                        requisitions = requisitions.Where(r => r.Request.Order.CreatedBy == userId ||
                            r.Request.Order.ContactPersonId == userId);
                    }
                    else if (model.FilterByContact.Value)
                    {
                        requisitions = requisitions.Where(r => r.Request.Order.ContactPersonId == userId);
                    }
                    else
                    {
                        requisitions = requisitions.Where(r => r.Request.Order.CreatedBy == userId);
                    }
                }
            }
            else if (brokerId.HasValue)
            {
                requisitions = requisitions.Where(r => r.Request.Ranking.BrokerId == brokerId);
            }
            else
            {
                return Forbid();
            }

            requisitions = model.Apply(requisitions);
            return View(
                new RequisitionListModel
                {
                    FilterModel = model,
                    Items = requisitions.Select(r => new RequisitionListItemModel
                    {
                        OrderRequestId = customerId.HasValue ? r.Request.OrderId : r.RequestId,
                        Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name,
                        OrderNumber = r.Request.Order.OrderNumber.ToString(),
                        OrderDateAndTime = new TimeRange
                        {
                            StartDateTime = r.Request.Order.StartAt,
                            EndDateTime = r.Request.Order.EndAt,
                        },
                        Status = r.Status,
                        Controller = customerId.HasValue ? "Order" : "Request",
                    })
                });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(RequisitionModel model)
        {
            if (ModelState.IsValid)
            {
                bool useRequestRows = false;
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    var request = _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.ContactPersonUser)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Ranking)
                    .Include(r => r.PriceRows)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                    .Single(o => o.RequestId == model.RequestId);

                    if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
                    {
                        var requisition = new Requisition
                        {
                            Status = RequisitionStatus.Created,
                            CreatedBy = User.GetUserId(),
                            CreatedAt = _clock.SwedenNow,
                            ImpersonatingCreatedBy = User.TryGetImpersonatorId(),
                            Message = model.Message,
                            SessionStartedAt = model.SessionStartedAt,
                            SessionEndedAt = model.SessionEndedAt,
                            TimeWasteNormalTime = model.TimeWasteTotalTime.HasValue ? (model.TimeWasteTotalTime ?? 0) - (model.TimeWasteIWHTime ?? 0) : model.TimeWasteTotalTime,
                            TimeWasteIWHTime = model.TimeWasteIWHTime,
                            InterpretersTaxCard = model.InterpreterTaxCard.Value,
                            PriceRows = new List<RequisitionPriceRow>(),
                            Attachments = model.Files?.Select(f => new RequisitionAttachment { AttachmentId = f.Id }).ToList()
                        };
                        var priceInformation = _priceCalculationService.GetPricesRequisition(
                            model.SessionStartedAt,
                            model.SessionEndedAt,
                            EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)request.CompetenceLevel),
                            request.Order.CustomerOrganisation.PriceListType,
                            request.Ranking.RankingId,
                            out useRequestRows,
                            (model.TimeWasteTotalTime ?? 0) - (model.TimeWasteIWHTime ?? 0),
                            model.TimeWasteIWHTime,
                            request.PriceRows.OfType<PriceRowBase>(),
                            model.Outlay,
                            model.PerDiem,
                            model.CarCompensation,
                            request.Order.ReplacingOrderId.HasValue ? request.Order.ReplacingOrder : null
                        );

                        requisition.RequestOrReplacingOrderPeriodUsed = useRequestRows;
                        requisition.PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequisitionPriceRow>(row)));
                        foreach (var tag in _dbContext.TemporaryAttachmentGroups.Where(t => t.TemporaryAttachmentGroupKey == model.FileGroupKey))
                        {
                            _dbContext.TemporaryAttachmentGroups.Remove(tag);
                        }
                        request.CreateRequisition(requisition);
                        _dbContext.SaveChanges();
                        var replacingRequisition = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.DeniedByCustomer &&
                            !r.ReplacedByRequisitionId.HasValue);
                        if (replacingRequisition != null)
                        {
                            replacingRequisition.ReplacedByRequisitionId = requisition.RequisitionId;
                            _dbContext.SaveChanges();
                        }
                        transaction.Commit();
                        _notificationService.RequisitionCreated(requisition);
                        return RedirectToAction("View", "Request", new { id = requisition.RequestId, tab = "requisition" });
                    }
                }
                return Forbid();
            }
            return View(nameof(Create), model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Approve(int requisitionId)
        {
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
                    requisition.Approve(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                    _notificationService.RequisitionApproved(requisition);
                    return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Deny(RequisitionDenyModel model)
        {
            if (ModelState.IsValid)
            {
                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.CreatedByUser)
                    .Single(r => r.RequisitionId == model.RequisitionId);
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    requisition.Deny(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                    _dbContext.SaveChanges();
                    _notificationService.RequisitionDenied(requisition);
                    return RedirectToAction("View", "Order", new { id = requisition.Request.OrderId, tab = "requisition" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.RequisitionId });
        }

        private PriceInformationModel GetRequisitionPriceInformation(Requisition requisition, bool useDisplayHideInfo = false)
        {
            if (requisition.PriceRows == null)
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = useDisplayHideInfo ? "Pris enligt tidigare rekvisition" : "Fakturainformation",
                UseDisplayHideInfo = useDisplayHideInfo
            };
        }

        private PriceInformationModel GetRequisitionPriceInformation(Request request)
        {
            if (request.PriceRows == null)
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris för avropssvar",
                UseDisplayHideInfo = true,
                Description = "Om rekvisitionen innehåller ersättning för bilersättning och traktamente kan förmedlingen komma att debitera påslag för sociala avgifter för de tolkar som inte är registrerade för F-skatt"
            };
        }
    }
}
