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

        public async Task<IActionResult> View(int id)
        {
            var requisition = _dbContext.Requisitions
                .Include(r => r.CreatedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.ProcessedUser).ThenInclude(u => u.CustomerOrganisation)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(pr => pr.PriceRows)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.PriceRows).ThenInclude(r => r.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
              .Single(o => o.RequisitionId == id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel.Value);
                var request = requisition.Request;
                var order = request.Order;
                var listType = order.CustomerOrganisation.PriceListType;
                var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
                var customerId = User.TryGetCustomerOrganisationId();
                model.AllowCreation = !customerId.HasValue && requisition.Request.Requisitions.All(r => r.Status == RequisitionStatus.DeniedByCustomer);
                model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(requisition.Request);
                model.RequestOrReplacingOrderPricesAreUsed = requisition.RequestOrReplacingOrderPeriodUsed;
                model.EventLog = new EventLogModel { Entries = EventLogHelper.GetEventLog(requisition).OrderBy(e => e.Timestamp).ToList() };
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var requisition = _dbContext.Requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
                .Include(r => r.Request).ThenInclude(r => r.PriceRows).ThenInclude(r => r.PriceListRow)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
             .Single(o => o.RequisitionId == id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
            {
                var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel.Value);
                var request = requisition.Request;
                var order = request.Order;
                var listType = order.CustomerOrganisation.PriceListType;
                var model = RequisitionProcessModel.GetProcessViewModelFromRequisition(requisition);
                model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(requisition.Request);
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
                .Include(r => r.Requisitions).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Include(r => r.PriceRows)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
            {
                var model = RequisitionModel.GetModelFromRequest(request);
                Guid groupKey = Guid.NewGuid();

                //Get request model from db
                 
                if (model.PreviousRequisition != null)
                {
                    var previousRequisition = _dbContext.Requisitions.SingleOrDefault(r => r.RequisitionId == model.PreviousRequisition.RequisitionId);
                    // Get the attachments from the previous requisition.
                    // Save a connection for all of these to Temp
                    foreach (var attachment in previousRequisition.Attachments)
                    {
                        _dbContext.TemporaryAttachmentGroups.Add(new TemporaryAttachmentGroup { TemporaryAttachmentGroupKey = groupKey, AttachmentId = attachment.AttachmentId, CreatedAt = _clock.SwedenNow, });
                    }
                    _dbContext.SaveChanges();
                    // Set the Files-list and the used FileGroupKey
                    List<FileModel> files = previousRequisition.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList();
                    model.Files = files.Count() > 0 ? files : null;
                }
                model.FileGroupKey = groupKey;
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
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

            if (!model.HasActiveFilters)
            {
                return View(
                new RequisitionListModel
                {
                    FilterModel = model,
                    Items = new List<RequisitionListItemModel>()
                });
            }
            var requisitions = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Where(r => !r.ReplacedByRequisitionId.HasValue);
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.
            var interpreterId = User.TryGetInterpreterId();
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
            else if (interpreterId.HasValue)
            {
                requisitions = requisitions.Where(r => r.Request.InterpreterId == interpreterId);
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
                        RequisitionId = r.RequisitionId,
                        Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name ?? "(Tolkanvändarutbildning)",
                        OrderNumber = r.Request.Order.OrderNumber.ToString(),
                        Start = r.Request.Order.StartAt,
                        End = r.Request.Order.EndAt,
                        Status = r.Status,
                        Action = customerId.HasValue && r.Status == RequisitionStatus.Created &&
                            (r.Request.Order.CreatedBy == userId ||
                             r.Request.Order.ContactPersonId == userId) ? nameof(Process) : nameof(View),
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
                            TimeWasteNormalTime = model.TimeWasteNormalTime,
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
                            model.TimeWasteNormalTime,
                            model.TimeWasteIWHTime,
                            request.PriceRows.OfType<PriceRowBase>(),
                            model.TravelCosts,
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
                        return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
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
            if (ModelState.IsValid)
            {
                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .Single(r => r.RequisitionId == requisitionId);
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    requisition.Approve(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                    _notificationService.RequisitionApproved(requisition);
                    return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = requisitionId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Deny(DenyMessageDialogModel model)
        {
            if (ModelState.IsValid)
            {
                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.CreatedByUser)
                    .Single(r => r.RequisitionId == model.ParentId);
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    requisition.Deny(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.Message);
                    _dbContext.SaveChanges();
                    _notificationService.RequisitionDenied(requisition);
                    return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Process), new { id = model.ParentId });
        }

        private PriceInformationModel GetRequisitionPriceInformation(Requisition requisition)
        {
            if (requisition.PriceRows == null)
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Fakturainformation",
                UseDisplayHideInfo = false
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
                UseDisplayHideInfo = true
            };
        }
    }
}
