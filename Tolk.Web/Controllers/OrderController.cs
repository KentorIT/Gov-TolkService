﻿using AutoMapper;
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
using Tolk.Web.Enums;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly OrderService _orderService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly INotificationService _notificationService;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IMapper _mapper;
        private readonly CacheService _cacheService;

        public OrderController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            OrderService orderService,
            DateCalculationService dateCalculationService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            IOptions<TolkOptions> options,
            INotificationService notificationService,
            UserManager<AspNetUser> usermanager,
            IMapper mapper,
            CacheService cacheService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _orderService = orderService;
            _dateCalculationService = dateCalculationService;
            _clock = clock;
            _logger = logger;
            _options = options?.Value;
            _notificationService = notificationService;
            _userManager = usermanager;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public IActionResult List()
        {
            return View(new OrderListModel
            {
                FilterModel = new OrderFilterModel
                {
                    IsCentralAdminOrOrderHandler = User.IsInRole(Roles.CentralAdministrator) || User.IsInRole(Roles.CentralOrderHandler),
                    IsAdmin = User.IsInRole(Roles.SystemAdministrator),
                    CustomerUnits = User.TryGetAllCustomerUnits()
                }
            });
        }

        public async Task<IActionResult> View(int id, string message = null, string errorMessage = null)
        {
            var order = await _dbContext.Orders.GetFullOrderById(id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {

                var allowEdit = (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded;
                var allowCancel = (await _authorizationService.AuthorizeAsync(User, order, Policies.Cancel)).Succeeded;
                var request = await _dbContext.Requests.GetActiveRequestByOrderId(id);

                var model = OrderViewModel.GetModelFromOrder(order);

                var orderStatusConfirmations = await _dbContext.OrderStatusConfirmation.GetStatusConfirmationsForOrder(id).ToListAsync();

                model.AllowOrderCancellation = allowCancel && (request?.CanCancel ?? false) && order.StartAt > _clock.SwedenNow;
                model.TimeIsValidForOrderReplacement = model.AllowOrderCancellation && TimeIsValidForOrderReplacement(order.StartAt);
                model.AllowReplacementOnCancel = model.AllowOrderCancellation && model.TimeIsValidForOrderReplacement && request != null && request.CanCreateReplacementOrderOnCancel;
                model.AllowNoAnswerConfirmation = allowEdit && order.Status == OrderStatus.NoBrokerAcceptedOrder && !orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder);
                model.AllowResponseNotAnsweredConfirmation = allowEdit && order.Status == OrderStatus.ResponseNotAnsweredByCreator && !orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator);
                model.AllowUpdateExpiry = order.OrderGroupId == null && order.Status == OrderStatus.AwaitingDeadlineFromCustomer && allowEdit;
                model.AllowEditContactPerson = order.Status != OrderStatus.CancelledByBroker && order.Status != OrderStatus.CancelledByCreator && order.Status != OrderStatus.NoBrokerAcceptedOrder && order.Status != OrderStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, order, Policies.EditContact)).Succeeded;
                model.AllowUpdate = _options.EnableOrderUpdate && order.Status == OrderStatus.ResponseAccepted && order.StartAt > _clock.SwedenNow && allowEdit;

                //Locations
                var interpreterLocations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(id).ToListAsync();

                model.RankedInterpreterLocationFirst = interpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation;
                model.RankedInterpreterLocationSecond = interpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation;
                model.RankedInterpreterLocationThird = interpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation;
                model.RankedInterpreterLocationFirstAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.Single(l => l.Rank == 1));
                model.RankedInterpreterLocationSecondAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.SingleOrDefault(l => l.Rank == 2));
                model.RankedInterpreterLocationThirdAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.SingleOrDefault(l => l.Rank == 3));

                //Compenteces
                var competenceRequirements = await _dbContext.OrderCompetenceRequirements
                    .GetOrderedCompetenceRequirementsForOrder(id)
                    .Select(r => new { r.CompetenceLevel })
                    .ToListAsync();

                model.RequestedCompetenceLevelFirst = competenceRequirements.FirstOrDefault()?.CompetenceLevel;
                model.RequestedCompetenceLevelSecond = competenceRequirements.Count > 1 ? competenceRequirements[1]?.CompetenceLevel : null;

                //LISTS
                model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForOrder(id, order.OrderGroupId), "Bifogade filer från myndighet");
                model.PreviousRequests = await BrokerListModel.GetFromList(_dbContext.Requests.GetLostRequestsForOrder(id));
                model.OrderCalculatedPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.OrderPriceRows.GetPriceRowsForOrder(id).ToListAsync(), PriceInformationType.Order);

                model.OrderRequirements = await OrderRequirementModel.GetFromList(_dbContext.OrderRequirements.GetRequirementsForOrder(id));
                model.Dialect = model.OrderRequirements.SingleOrDefault(r => r.RequirementType == RequirementType.Dialect)?.RequirementDescription;

                if (request != null)
                {
                    //MIGHT MOVE TO MODEL BUILDER

                    model.AllowConfirmCancellation = allowEdit && order.Status == OrderStatus.CancelledByBroker && !_dbContext.RequestStatusConfirmation
                        .GetStatusConfirmationsForRequest(request.RequestId).Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker);
                    model.RequestStatus = request.Status;
                    model.BrokerName = request.Ranking.Broker.Name;
                    model.BrokerOrganizationNumber = request.Ranking.Broker.OrganizationNumber;
                    //don't use AnsweredBy since request for replacement order can have interpreter etc but not is answered
                    model.ActiveRequestIsAnswered = request.InterpreterBrokerId != null && (request.Status != RequestStatus.Created && request.Status != RequestStatus.Received);
                    model.AllowRequestPrint = request.CanPrint && (await _authorizationService.AuthorizeAsync(User, order, Policies.Print)).Succeeded;
                    //Move to Extension, with Dto
                    var requestChecks = await _dbContext.Requests
                        .Where(r => r.RequestId == request.RequestId)
                        .Select(r => new
                        {
                            LatestComplaint = r.Complaints.Max(c => (int?)c.ComplaintId),
                            LatestRequisition = r.Requisitions.Max(req => (int?)req.RequisitionId),
                            CanCreateRequisitions = !r.Requisitions.Any(req => req.Status == RequisitionStatus.Reviewed || req.Status == RequisitionStatus.Created),
                            HasConstraints = r.Complaints.Any(),
                        }).SingleAsync();
                    if (model.ActiveRequestIsAnswered)
                    {

                        model.CancelMessage = request.CancelMessage;
                        model.ActiveRequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync(), PriceInformationType.Request);
                        model.RequestId = request.RequestId;
                        model.AnsweredBy = request.AnsweringUser?.CompleteContactInformation;
                        model.ExpectedTravelCostInfo = request.ExpectedTravelCostInfo;
                        //There is no InterpreterLocation for replacement order if not answered yet
                        if (request.InterpreterLocation.HasValue)
                        {
                            model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                        }
                        model.InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel)request.CompetenceLevel;
                        model.InterpreterName = request.Interpreter?.CompleteContactInformation;
                        model.AllowComplaintCreation = !requestChecks.HasConstraints && request.IsApprovedOrDelivered &&
                            order.StartAt < _clock.SwedenNow && (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded;
                        model.RequestAttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequest(request.RequestId, request.RequestGroupId), "Bifogade filer från förmedling");
                        model.AllowProcessing = AllowProcessing(order, model) && (await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded;
                        model.TerminateOnDenial = request.TerminateOnDenial;
                    }
                    model.ActiveRequest = RequestViewModel.GetModelFromRequest(request, order.AllowExceedingTravelCost);
                    model.ActiveRequest.ComplaintId = requestChecks.LatestComplaint;
                    model.ActiveRequest.RequisitionId = requestChecks.LatestRequisition;
                    model.ActiveRequest.AllowRequisitionRegistration = requestChecks.CanCreateRequisitions;
                    model.ActiveRequest.RequirementAnswers = await RequestRequirementAnswerModel.GetFromList(_dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(request.RequestId));
                    model.ActiveRequest.RequestCalculatedPriceInformationModel = model.ActiveRequestPriceInformationModel;
                }
                else
                {
                    model.ActiveRequest = new RequestViewModel();
                }
                model.ActiveRequest.LanguageAndDialect = model.LanguageAndDialect;
                model.ActiveRequest.RegionName = model.RegionName;
                model.ActiveRequest.TimeRange = model.TimeRange;
                model.ActiveRequest.IsCancelled = model.Status == OrderStatus.CancelledByCreator || model.Status == OrderStatus.CancelledByBroker;

                model.EventLog = new EventLogModel
                {
                    Header = "Bokningshändelser",
                    Id = "EventLog_Order",
                    DynamicLoadPath = $"Order/{nameof(GetEventLog)}/{id}",
                };
                model.InfoMessage = message;
                model.ErrorMessage = errorMessage;
                return View(model);
            }
            return Forbid();
        }

        private bool TimeIsValidForOrderReplacement(DateTimeOffset orderStart)
        {
            var noOfDays = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(_clock.SwedenNow.DateTime, orderStart.DateTime);
            return noOfDays > -1 && noOfDays < 2;
        }

        private static bool AllowProcessing(Order o, OrderViewModel model) => model.ActiveRequestIsAnswered && (AllowProcessingOrderBelongsToGroup(o, model) || AllowProcessingOrderNotBelongsToGroup(o, model));

        private static bool AllowProcessingOrderBelongsToGroup(Order o, OrderViewModel model) => o.OrderGroupId.HasValue && model.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed;

        private static bool AllowProcessingOrderNotBelongsToGroup(Order o, OrderViewModel model) => !o.OrderGroupId.HasValue && (model.RequestStatus == RequestStatus.Accepted || model.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed);

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Replace(int replacingOrderId, string cancelMessage)
        {
            var order = GetOrder(replacingOrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Replace)).Succeeded)
            {
                if (order.ActiveRequest.CanCreateReplacementOrderOnCancel && TimeIsValidForOrderReplacement(order.StartAt))
                {
                    ReplaceOrderModel model = _mapper.Map<ReplaceOrderModel>(OrderModel.GetModelFromOrder(order));
                    model.ReplacedTimeRange = new TimeRange
                    {
                        StartDateTime = order.StartAt,
                        EndDateTime = order.EndAt
                    };
                    model.OrderId = null;
                    model.ReplacingOrderNumber = order.OrderNumber;
                    model.ReplacingOrderId = replacingOrderId;
                    model.CancelMessage = cancelMessage;
                    //Set the Files-list and the used FileGroupKey
                    List<FileModel> files = order.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList();
                    model.Files = files.Any() ? files : null;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det gick inte att skapa ett ersättningsuppdrag för uppdraget, så ingen avbokning kunde heller ske" });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Replace(ReplaceOrderModel model)
        {
            if (ModelState.IsValid)
            {
                Order order = GetOrder(model.ReplacingOrderId.Value);
                if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Replace)).Succeeded)
                {
                    if (order.ActiveRequest.CanCreateReplacementOrderOnCancel && TimeIsValidForOrderReplacement(order.StartAt))
                    {
                        using (var trn = await _dbContext.Database.BeginTransactionAsync())
                        {
                            Order replacementOrder = new Order(order);
                            model.UpdateOrder(replacementOrder, model.TimeRange.StartDateTime.Value, model.TimeRange.EndDateTime.Value, isReplace: true);
                            await _orderService.ReplaceOrder(order, replacementOrder, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                            await _dbContext.SaveChangesAsync();
                            trn.Commit();
                            return RedirectToAction("Index", "Home", new { message = "Ersättningsuppdrag är skickat" });
                        }
                    }
                    else
                    {
                        return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = "Det gick inte att skapa ett ersättningsuppdrag för uppdraget, så ingen avbokning kunde heller ske" });
                    }
                }
            }
            return View(model);
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Update(int id)
        {
            var order = GetOrder(id);

            if (_options.EnableOrderUpdate && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                var request = order.Requests.SingleOrDefault(r =>
                                        r.Status != RequestStatus.InterpreterReplaced &&
                                        r.Status != RequestStatus.DeniedByTimeLimit &&
                                        r.Status != RequestStatus.DeniedByCreator &&
                                        r.Status != RequestStatus.DeclinedByBroker &&
                                        r.Status != RequestStatus.LostDueToQuarantine &&
                                        r.Status != RequestStatus.ResponseNotAnsweredByCreator);
                UpdateOrderModel model = _mapper.Map<UpdateOrderModel>(OrderModel.GetModelFromOrder(order, request?.RequestId));
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                List<FileModel> files = order.Attachments.Select(a => new FileModel
                {
                    Id = a.Attachment.AttachmentId,
                    FileName = a.Attachment.FileName,
                    Size = a.Attachment.Blob.Length
                }).Union(order.Group?.Attachments
                .Where(oa => !oa.Attachment.OrderAttachmentHistoryEntries.Any(h => h.OrderGroupAttachmentRemoved && h.OrderChangeLogEntry.OrderId == order.OrderId))
                .Select(a => new FileModel
                {
                    Id = a.Attachment.AttachmentId,
                    FileName = a.Attachment.FileName,
                    Size = a.Attachment.Blob.Length
                }) ?? Enumerable.Empty<FileModel>()).ToList();

                model.Files = files.Any() ? files : null;
                model.SelectedInterpreterLocation = (InterpreterLocation)request.InterpreterLocation.Value;
                if (model.SelectedInterpreterLocation == InterpreterLocation.OffSitePhone || model.SelectedInterpreterLocation == InterpreterLocation.OffSiteVideo)
                {
                    model.OffSiteContactInformation = order.InterpreterLocations.Where(i => i.InterpreterLocation == model.SelectedInterpreterLocation).Single().OffSiteContactInformation;
                }
                else
                {
                    model.LocationCity = order.InterpreterLocations.Where(i => i.InterpreterLocation == model.SelectedInterpreterLocation).Single().City;
                    model.LocationStreet = order.InterpreterLocations.Where(i => i.InterpreterLocation == model.SelectedInterpreterLocation).Single().Street;
                }
                model.ActiveRequest = RequestModel.GetModelFromRequest(request, true);
                model.ContactPersonId = order.ContactPersonId;
                model.ActiveRequest.OrderModel = model;
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Update(UpdateOrderModel model)
        {
            if (ModelState.IsValid)
            {
                Order order = GetOrder(model.OrderId.Value);
                if (_options.EnableOrderUpdate && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
                {
                    try
                    {
                        var orderFieldsUpdated = false;
                        var attachmentChanged = false;
                        var contactpersonChanged = false;
                        AspNetUser oldContactPerson = order.ContactPersonUser;

                        IEnumerable<int> oldOrderAttachmentIdsToCompare = order.Attachments
                            .Select(a => a.AttachmentId).Union(order.OrderGroupId.HasValue ? order.Group.Attachments
                            .Where(oa => !oa.Attachment.OrderAttachmentHistoryEntries.Any(h => h.OrderGroupAttachmentRemoved && h.OrderChangeLogEntry.OrderId == order.OrderId))
                            .Select(ag => ag.AttachmentId) : Enumerable.Empty<int>());
                        IEnumerable<int> updatedAttachments = (model.Files?.Any() ?? false) ? model.Files.Select(a => a.Id) : Enumerable.Empty<int>();

                        //check if attachments are changed
                        if (!oldOrderAttachmentIdsToCompare.OrderBy(r => r).SequenceEqual(updatedAttachments.OrderBy(r => r)))
                        {
                            attachmentChanged = true;

                        }
                        //check if contactperson is changed
                        if (order.ContactPersonId != model.ContactPersonId)
                        {
                            contactpersonChanged = true;
                            ChangeContactPerson(order, model.ContactPersonId);
                        }
                        //check if something else is updated
                        if (model.IsOrderUpdated(order))
                        {
                            orderFieldsUpdated = true;

                        }
                        if (!(orderFieldsUpdated || attachmentChanged || contactpersonChanged))
                        {
                            return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = "OBS! Det fanns inga ändringar att spara på bokningen!" });
                        }
                        if (orderFieldsUpdated || attachmentChanged)
                        {
                            order.Update(new ChangeOrderModel
                            {
                                UpdatedAt = _clock.SwedenNow,
                                UpdatedBy = User.GetUserId(),
                                ImpersonatedUpdatedBy = User.TryGetImpersonatorId(),
                                Description = model.Description,
                                LocationStreet = model.LocationStreet,
                                OffSiteContactInformation = model.OffSiteContactInformation,
                                CustomerDepartment = model.UnitName,
                                CustomerReferenceNumber = model.CustomerReferenceNumber,
                                InvoiceReference = model.InvoiceReference,
                                OrderChangeLogType = (orderFieldsUpdated && attachmentChanged) ? OrderChangeLogType.AttachmentAndOrderInformationFields : attachmentChanged ? OrderChangeLogType.Attachment : OrderChangeLogType.OrderInformationFields,
                                SelectedInterpreterLocation = model.SelectedInterpreterLocation,
                                Attachments = updatedAttachments
                            });

                        }
                        await _dbContext.SaveChangesAsync();
                        order = GetOrder(model.OrderId.Value);
                        if (orderFieldsUpdated || attachmentChanged)
                        {
                            _notificationService.OrderUpdated(order, attachmentChanged, orderFieldsUpdated);
                        }
                        if (contactpersonChanged)
                        {
                            _notificationService.OrderContactPersonChanged(order, oldContactPerson);
                        }
                        return RedirectToAction(nameof(View), new { id = order.OrderId, message = "Bokningen är nu ändrad" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = ex.Message });
                    }
                }
                return Forbid();
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetEventLog(int id)
        {
            Order order = GetOrder(id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = EventLogHelper.GetEventLog(order, order.Requests.All(r => r.Status == RequestStatus.DeclinedByBroker || r.Status == RequestStatus.DeniedByTimeLimit)
                        ? order.Requests.OrderBy(r => r.RequestId).Last()
                        : null)
                            .OrderBy(e => e.Timestamp)
                            .ThenBy(e => e.Weight)
                            .ToList()
                });
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Create()
        {
            var now = _clock.SwedenNow.DateTime;
            var firstWorkDay = _dateCalculationService.GetFirstWorkDay(now).Date;
            var panicTime = _dateCalculationService.GetFirstWorkDay(firstWorkDay).Date;
            if (now.Hour >= 14)
            {
                //Add day if after 14...
                panicTime = _dateCalculationService.GetFirstWorkDay(panicTime.AddDays(1).Date).Date;
            }
            DateTime nextPanicTime = _dateCalculationService.GetFirstWorkDay(panicTime.AddDays(1).Date).Date;

            var user = await _userManager.Users
                .Include(u => u.DefaultSettings)
                .Include(u => u.DefaultSettingOrderRequirements)
                .Include(u => u.CustomerUnits).ThenInclude(c => c.CustomerUnit)
                .SingleOrDefaultAsync(u => u.Id == User.GetUserId());

            var model = new OrderModel()
            {
                LastTimeForRequiringLatestAnswerBy = panicTime.ToSwedishString("yyyy-MM-dd"),
                NextLastTimeForRequiringLatestAnswerBy = nextPanicTime.ToSwedishString("yyyy-MM-dd"),
                CreatedByName = user.FullName,
                UserDefaultSettings = DefaultSettingsModel.GetModel(user),
                EnableOrderGroups = _options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == User.GetCustomerOrganisationId() && c.UseOrderGroups)
            };
            model.UpdateModelWithDefaultSettings(user.CustomerUnits.Where(cu => cu.CustomerUnit.IsActive).Select(cu => cu.CustomerUnitId).ToList());
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Add(OrderModel model)
        {
            if (model.SeveralOccasions)
            {
                ModelState.Remove("SplitTimeRange.StartDate");
                ModelState.Remove("SplitTimeRange.StartTimeHour");
                ModelState.Remove("SplitTimeRange.StartTimeMinutes");
                ModelState.Remove("SplitTimeRange.EndTimeHour");
                ModelState.Remove("SplitTimeRange.EndTimeMinutes");
                model.SplitTimeRange = null;
            }
            if (ModelState.IsValid)
            {
                using (var trn = await _dbContext.Database.BeginTransactionAsync())
                {
                    if (model.IsMultipleOrders)
                    {
                        var orderGroup = CreateNewOrderGroup(GetOrdersForGroup(model).ToList());
                        model.UpdateOrderGroup(orderGroup);
                        await _dbContext.AddAsync(orderGroup);
                        //TODO: LASTANSWER BY HAS TO BE NULL IF NOT ONLY ONE OCCASION WITH EXTRA INTERPRETER!!
                        await _orderService.CreateRequestGroup(orderGroup, latestAnswerBy: model.LatestAnswerBy);

                        await _dbContext.SaveChangesAsync();
                        trn.Commit();
                        return RedirectToAction(nameof(SentGroup), new { id = orderGroup.OrderGroupId });
                    }
                    else
                    {
                        Order order = CreateNewOrder();
                        var firstOccasion = model.FirstOccasion;
                        model.UpdateOrder(order, firstOccasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), firstOccasion.OccasionEndDateTime.ToDateTimeOffsetSweden());
                        await _dbContext.AddAsync(order);
                        await _dbContext.SaveChangesAsync(); // Save changes to get id for event log

                        await _orderService.CreateRequest(order, latestAnswerBy: model.LatestAnswerBy);

                        await _dbContext.SaveChangesAsync();
                        trn.Commit();
                        return RedirectToAction(nameof(Sent), new { id = order.OrderId });
                    }
                }
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public ActionResult Confirm(OrderModel model)
        {
            Order order = CreateNewOrder();
            PriceListType pricelistType = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == order.CustomerOrganisation.CustomerOrganisationId).PriceListType;
            OrderViewModel updatedModel = null;
            var firstOccasion = model.FirstOccasion;
            string warningOrderTimeInfo = string.Empty;
            model.UpdateOrder(order, firstOccasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), firstOccasion.OccasionEndDateTime.ToDateTimeOffsetSweden());
            updatedModel = OrderViewModel.GetModelFromOrderForConfirmation(order);
            if (model.IsMultipleOrders)
            {
                updatedModel.OrderOccasionDisplayModels = GetGroupOrders(model, pricelistType);
                updatedModel.SeveralOccasions = true;
                updatedModel.WarningOrderGroupCloseInTime = CheckOrderGroupCloseInTime(updatedModel.OrderOccasionDisplayModels);
                warningOrderTimeInfo = CheckReasonableDurationTimeOrderGroup(updatedModel.OrderOccasionDisplayModels);
                updatedModel.WarningOrderTimeInfo = string.IsNullOrEmpty(warningOrderTimeInfo) ?
                    CheckOrderOccasionFarAway(updatedModel.OrderOccasionDisplayModels.OrderBy(oo => oo.OccasionStartDateTime).Last().OccasionStartDateTime, true) :
                    $"{warningOrderTimeInfo} {CheckOrderOccasionFarAway(updatedModel.OrderOccasionDisplayModels.OrderBy(oo => oo.OccasionStartDateTime).Last().OccasionStartDateTime, true)}";
            }
            else
            {
                //get pricelisttype for customer and get calculated price
                updatedModel.OrderCalculatedPriceInformationModel = new PriceInformationModel
                {
                    MealBreakIsNotDetucted = order.MealBreakIncluded ?? false,
                    Header = "Beräknat preliminärt pris",
                    PriceInformationToDisplay = _orderService.GetOrderPriceinformationForConfirmation(order, pricelistType),
                    UseDisplayHideInfo = true,
                    Description = "Om inget krav eller önskemål om specifik kompetensnivå har angetts i bokningsförfrågan beräknas kostnaden enligt taxan för arvodesnivå Auktoriserad tolk. Slutlig arvodesnivå kan då avvika beroende på vilken tolk som tillsätts enligt principen för kompetensprioritering."
                };
                warningOrderTimeInfo = CheckReasonableDurationTime(order.StartAt.DateTime, order.EndAt.DateTime);
                updatedModel.WarningOrderTimeInfo = string.IsNullOrEmpty(warningOrderTimeInfo) ? CheckOrderOccasionFarAway(order.StartAt.DateTime) :
                    $"{warningOrderTimeInfo} {CheckOrderOccasionFarAway(order.StartAt.DateTime)}";
                updatedModel.DisplayMealBreakIncludedText = order.MealBreakTextToDisplay;
            }
            var customerUnit = model.CustomerUnitId.HasValue && model.CustomerUnitId > 0 ? _dbContext.CustomerUnits
                .Single(cu => cu.CustomerUnitId == model.CustomerUnitId) : null;

            order.CustomerUnit = customerUnit;

            updatedModel.RegionName = _dbContext.Regions
                .Single(r => r.RegionId == model.RegionId).Name;

            updatedModel.CustomerUnitName = model.CustomerUnitId.HasValue ?
                model.CustomerUnitId == 0 ? "Bokningen ska inte kopplas till någon enhet" :
                customerUnit.Name : "Du tillhör ingen enhet i systemet";
            Language language = _dbContext.Languages
                .Single(l => l.LanguageId == model.LanguageId);

            updatedModel.LanguageName = order.OtherLanguage ?? language.Name;
            updatedModel.LatestAnswerBy = model.LatestAnswerBy;
            updatedModel.WarningOrderRequiredCompetenceInfo = CheckOrderCompetenceRequirements(order, language);

            if (order.Attachments?.Count > 0)
            {
                List<FileModel> attachments = new List<FileModel>();
                foreach (int attId in order.Attachments.Select(a => a.AttachmentId))
                {
                    Attachment a = _dbContext.Attachments.Single(f => f.AttachmentId == attId);
                    attachments.Add(new FileModel { FileName = a.FileName, Id = a.AttachmentId, Size = a.Blob.Length });
                }
                updatedModel.AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer",
                    DisplayFiles = attachments
                };
            }

            var user = _userManager.Users.Where(u => u.Id == User.GetUserId()).Single();
            updatedModel.ContactPerson = order.ContactPersonId.HasValue ? _userManager.Users.Where(u => u.Id == order.ContactPersonId).Single().CompleteContactInformation : string.Empty;
            updatedModel.CreatedBy = order.ContactInformation;
            updatedModel.CustomerName = user.CustomerOrganisation.Name;
            updatedModel.CustomerOrganisationNumber = user.CustomerOrganisation.OrganisationNumber;
            return PartialView(nameof(Confirm), updatedModel);
        }

        private static string CheckReasonableDurationTimeOrderGroup(IEnumerable<OrderOccasionDisplayModel> orderOccasionDisplayModels)
        {
            string message = string.Empty;
            foreach (OrderOccasionDisplayModel orderOccasion in orderOccasionDisplayModels)
            {
                message = CheckReasonableDurationTime(orderOccasion.OccasionStartDateTime, orderOccasion.OccasionEndDateTime, true);
                if (!string.IsNullOrEmpty(message))
                {
                    return message;
                }
            }
            return message;
        }

        private static string CheckReasonableDurationTime(DateTime start, DateTime end, bool isOrderGroup = false)
        {
            int minutes = (int)(end - start).TotalMinutes;
            return minutes > 600 ? isOrderGroup ?
                $"Observera att tiden för minst ett tillfälle är längre än normalt ({start.ToSwedishString("yyyy-MM-dd HH:mm")}-{end.ToSwedishString("HH:mm")}), för att ändra tiden gå tillbaka till föregående steg, om angiven tid är korrekt kan bokningen skickas som vanligt." :
                "Observera att tiden för tolkuppdraget är längre än normalt, för att ändra tiden gå tillbaka till föregående steg, om angiven tid är korrekt kan bokningen skickas som vanligt." :
                minutes < 60 ? isOrderGroup ?
                $"Observera att tiden för minst ett tillfälle är kortare än normalt ({start.ToSwedishString("yyyy-MM-dd HH:mm")}-{end.ToSwedishString("HH:mm")}), för att ändra tiden gå tillbaka till föregående steg, om angiven tid är korrekt kan bokningen skickas som vanligt." :
                "Observera att tiden för tolkuppdraget är kortare än normalt, för att ändra tiden gå tillbaka till föregående steg, om angiven tid är korrekt kan bokningen skickas som vanligt." :
                string.Empty;
        }

        private string CheckOrderOccasionFarAway(DateTime orderStart, bool isOrderGroup = false)
        {
            return orderStart.AddYears(-2) > _clock.SwedenNow.DateTime ? isOrderGroup ?
                $"Observera att tiden för minst ett tillfälle ligger långt fram i tiden (startdatum: {orderStart.ToSwedishString("yyyy-MM-dd")}), för att ändra tiden gå tillbaka till föregående steg, om angiven tid är korrekt kan bokningen skickas som vanligt." :
                "Observera att tiden för tolkuppdraget ligger långt fram i tiden, för att ändra tiden gå tillbaka till föregående steg, om angiven tid är korrekt kan bokningen skickas som vanligt." :
                string.Empty;
        }

        private string CheckOrderGroupCloseInTime(IEnumerable<OrderOccasionDisplayModel> orderOccasionDisplayModels)
        {
            if (orderOccasionDisplayModels.Count() == 2 && orderOccasionDisplayModels.Any(o => o.ExtraInterpreter))
                return string.Empty;
            var firstOrderStart = orderOccasionDisplayModels.OrderBy(oo => oo.OccasionStartDateTime).First().OccasionStartDateTime;
            return firstOrderStart < _clock.SwedenNow.AddDays(7) ?
                $"Observera att tiden för minst ett tillfälle ligger nära i tiden (startdatum: {firstOrderStart.ToSwedishString("yyyy-MM-dd")}), så det finns risk att förmedlingen inte hinner tillsätta tolk till samtliga tillfällen och då måste tacka nej till hela bokningen." : string.Empty;
        }

        private static string CheckOrderCompetenceRequirements(Order o, Language l)
        {
            return (!o.SpecificCompetenceLevelRequired || !o.LanguageHasAuthorizedInterpreter || l.HasAllCompetences) ?
            string.Empty :
                    ((!l.HasLegal && o.CompetenceRequirements.Any(oc => oc.CompetenceLevel == CompetenceAndSpecialistLevel.CourtSpecialist)) ||
                    (!l.HasHealthcare && o.CompetenceRequirements.Any(oc => oc.CompetenceLevel == CompetenceAndSpecialistLevel.HealthCareSpecialist)) ||
                    (!l.HasAuthorized && o.CompetenceRequirements.Any(oc => oc.CompetenceLevel == CompetenceAndSpecialistLevel.AuthorizedInterpreter)) ||
                    (!l.HasEducated && o.CompetenceRequirements.Any(oc => oc.CompetenceLevel == CompetenceAndSpecialistLevel.EducatedInterpreter))) ?
                    "Observera att du har ställt krav på minst en kompetensnivå där det för närvarande saknas tolkar för det valda språket i Kammarkollegiets tolkregister. Det finns risk för att förmedlingen inte kan tillsätta någon tolk."
                    : string.Empty;
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> SentGroup(int id)
        {
            OrderGroup orderGroup = await _dbContext.OrderGroups
                .Include(o => o.Orders).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .SingleAsync(o => o.OrderGroupId == id);

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                return View(new OrderGroupSummaryModel
                {
                    OrderGroupNumber = orderGroup.OrderGroupNumber,
                    OrderOccasionDisplayModels = orderGroup.Orders
                        .Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o, PriceInformationModel.GetPriceinformationToDisplay(o, initialCollapse: false)))
                });
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Sent(int id)
        {
            Order order = GetOrder(id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                var model = OrderModel.GetModelFromOrder(order);
                model.OrderCalculatedPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(order);
                model.OrderCalculatedPriceInformationModel.CenterHeader = true;
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Print(int id)
        {
            var order = await _dbContext.Orders.GetFullOrderById(id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Print)).Succeeded)
            {
                var request = await _dbContext.Requests.GetActiveRequestByOrderId(id, false);
                if (!(request?.CanPrint ?? false))
                {
                    return RedirectToAction(nameof(View), new { id, errorMessage = "Bokningen har fel status för att skriva ut en bokningsbekräftelse" });
                }

                var model = OrderViewModel.GetModelFromOrder(order);
                model.BrokerName = request.Ranking.Broker.Name;
                model.CreatedBy = request.Order.CreatedByUser.FullName;
                model.ActiveRequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(request);
                model.RequestId = request.RequestId;
                model.AnsweredBy = request.AnsweringUser?.FullName;
                model.AnsweredAt = request.AnswerDate;
                model.ExpectedTravelCostInfo = request.ExpectedTravelCostInfo;
                model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                model.InterpreterLocationInfoAnswer = GetInterpreterLocationInfoAnswer(order, request.InterpreterLocation.Value);
                model.InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel)request.CompetenceLevel;
                model.ActiveRequest = RequestViewModel.GetModelFromRequest(request, order.AllowExceedingTravelCost);
#warning add pertinent lists, both request and order!!!
                model.ActiveRequest.InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null;
                model.ActiveRequest.Interpreter = request.Interpreter.FullName;
#warning add inerpreter info!
                //model.ActiveRequest.NewInterpreterEmail = request.Interpreter.Email ?? "-";
                //model.ActiveRequest.NewInterpreterPhoneNumber = request.Interpreter.PhoneNumber ?? "-";
                //model.ActiveRequest.NewInterpreterOfficialInterpreterId = request.Interpreter.OfficialInterpreterId ?? "-";
                model.Dialect = GetRequestAnswerDialect(model.Dialect, model.ActiveRequest.RequirementAnswers);
                model.ActiveRequest.AnswerProcessedBy = request.AnswerProcessedBy.HasValue ? request.ProcessingUser.FullName : "Systemet";
                model.ActiveRequest.AnswerProcessedAt = request.AnswerProcessedAt.HasValue ? request.AnswerProcessedAt.Value.ToSwedishString("yyyy-MM-dd HH:mm") : request.AnswerDate.Value.ToSwedishString("yyyy-MM-dd HH:mm");
                return View(model);
            }
            return Forbid();
        }

        private static string GetRequestAnswerDialect(string dialect, List<RequestRequirementAnswerModel> requirementAnswers)
        {
            if (!string.IsNullOrEmpty(dialect) && requirementAnswers != null && requirementAnswers.Any(or => or.RequirementType == RequirementType.Dialect))
            {
                var reqDialect = requirementAnswers.Single(or => or.RequirementType == RequirementType.Dialect);
                return reqDialect.CanMeetRequirement ? $"(dialekt: {reqDialect.Description})" : string.Empty;
            }
            return string.Empty;
        }

        private static string GetInterpreterLocationInfoAnswer(Order o, int locationAnswer)
        {
            foreach (OrderInterpreterLocation i in o.InterpreterLocations)
            {
                if ((int)i.InterpreterLocation == locationAnswer)
                {
                    return (i.InterpreterLocation == InterpreterLocation.OffSitePhone || i.InterpreterLocation == InterpreterLocation.OffSiteVideo) ? $"Kontaktinformation: {i.OffSiteContactInformation}" : $"Adress: {i.FullAddress}";
                }
            }
            return string.Empty;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Approve(ProcessRequestModel model)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestId == model.RequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request.Order, Policies.Accept)).Succeeded)
            {
                if (!request.CanApprove)
                {
                    _logger.LogWarning("Wrong status when trying to Approve request. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction(nameof(View), new { id = request.OrderId });
                }
                _orderService.ApproveRequestAnswer(request, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = request.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Cancel(CancelOrderModel model)
        {
            var order = _dbContext.Orders
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Single(o => o.OrderId == model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Cancel)).Succeeded)
            {
                if (order.ActiveRequest == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Uppdraget kunde inte avbokas" });
                }
                if (model.AddReplacementOrder)
                {
                    if (order.ActiveRequest.CanCreateReplacementOrderOnCancel && TimeIsValidForOrderReplacement(order.StartAt))
                    {
                        //Forward the message to replace
                        return RedirectToAction(nameof(Replace), new { replacingOrderId = model.OrderId, cancelMessage = model.CancelMessage });
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Det gick inte att skapa ett ersättningsuppdrag för uppdraget, så ingen avbokning kunde heller ske" });
                    }
                }
                _orderService.CancelOrder(order, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = model.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmCancellation(int requestId)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .SingleAsync(r => r.RequestId == requestId);

            if ((await _authorizationService.AuthorizeAsync(User, request.Order, Policies.View)).Succeeded)
            {
                if (request.Status != RequestStatus.CancelledByBroker)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det fanns ingen avbokning att bekräfta på denna bokning." });
                }
                await _orderService.ConfirmCancellationByBroker(request, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = "Avbokning är bekräftad" });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmNoAnswer(int orderId)
        {
            var order = await _dbContext.Orders.Include(o => o.OrderStatusConfirmations).SingleAsync(o => o.OrderId == orderId);
            if (order.Status == OrderStatus.NoBrokerAcceptedOrder && (await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                try
                {
                    await _orderService.ConfirmNoAnswer(order, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmResponseNotAnswered(int orderId)
        {
            var order = await _dbContext.Orders.Include(o => o.OrderStatusConfirmations).SingleAsync(o => o.OrderId == orderId);
            if (order.Status == OrderStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                try
                {
                    await _orderService.ConfirmResponeNotAnswered(order, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Deny(ProcessRequestModel model)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(req => req.Ranking)
                .SingleAsync(r => r.RequestId == model.RequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request.Order, Policies.Accept)).Succeeded)
            {
                if (!request.CanDeny)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det går inte att underkänna denna tillsättning" });
                }
                var requestWillTerminate = request.TerminateOnDenial;
                await _orderService.DenyRequestAnswer(request, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = request.OrderId, message = requestWillTerminate ? "Tillsättning är nu underkänd och bokningsförfrågan avslutad" : string.Empty });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ChangeContactPerson(OrderChangeContactPersonModel model)
        {
            var order = GetOrder(model.OrderId);
            var oldContactPerson = order.ContactPersonUser;
            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.EditContact)).Succeeded)
            {
                if (model.ContactPersonId == order.ContactPersonId)
                {
                    return RedirectToAction(nameof(View), new { id = order.OrderId });
                }
                ChangeContactPerson(order, model.ContactPersonId);
                await _dbContext.SaveChangesAsync();
                _notificationService.OrderContactPersonChanged(order, oldContactPerson);
                if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
                {
                    return RedirectToAction(nameof(View), new { id = order.OrderId, message = $"Person med rätt att granska rekvisition för bokningen är ändrad" });
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { message = $"Person med rätt att granska rekvisition för bokning {order.OrderNumber} är ändrad" });
                }
            }
            return Forbid();
        }

        private void ChangeContactPerson(Order order, int? newContactPersonId)
        {
            order.ChangeContactPerson(_clock.SwedenNow, User.GetUserId(),
                User.TryGetImpersonatorId(), _dbContext.Users.SingleOrDefault(u => u.Id == newContactPersonId));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> UpdateExpiry(int orderId, DateTimeOffset latestAnswerBy)
        {
            var order = GetOrder(orderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                var request = order.Requests.SingleOrDefault(r => r.Status == RequestStatus.AwaitingDeadlineFromCustomer);
                if (request == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Denna bokning behöver inte få sista svarstid satt." });
                }

                _orderService.SetRequestExpiryManually(request, latestAnswerBy, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = $"Sista svarstid för bokning {order.OrderNumber} är satt" });
            }
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> ListOrders(IDataTablesRequest request)
        {
            var model = new OrderFilterModel();
            await TryUpdateModelAsync(model);
            model.UserId = User.GetUserId();
            model.IsCentralAdminOrOrderHandler = User.IsInRole(Roles.CentralAdministrator) || User.IsInRole(Roles.CentralOrderHandler);
            model.IsAdmin = User.IsInRole(Roles.SystemAdministrator);
            model.CustomerUnits = User.TryGetAllCustomerUnits();

            if (!model.IsAdmin)
            {
                model.CustomerOrganisationId = User.TryGetCustomerOrganisationId();
            }

            var entities = model.GetEntities(_dbContext.OrderListRows.Select(o => o));
            var filteredData = model.Apply(entities);
            return AjaxDataTableHelper.GetData(request, entities.Count(), filteredData, d => d.Select(o => new OrderListItemModel
            {
                EntityId = o.EntityId,
                LanguageName = o.LanguageName,
                OrderNumber = o.EntityNumber,
                ParentOrderNumber = o.EntityParentNumber,
                RegionName = o.RegionName,
                Status = o.Status,
                CreatorName = o.CreatorName,
                BrokerName = o.BrokerName,
                CustomerName = o.CustomerName,
                StartAt = o.StartAt,
                EndAt = o.EndAt,
                LinkOverride = o.RowType == OrderRowType.OrderGroup ? "/OrderGroup/View" : string.Empty

            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<OrderListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(OrderListItemModel.CustomerName)).Visible = User.IsInRole(Roles.SystemAdministrator);
            definition.Single(d => d.Name == nameof(OrderListItemModel.CreatorName)).Visible = User.IsInRole(Roles.CentralAdministrator) || User.IsInRole(Roles.CentralOrderHandler);
            return Json(definition);
        }

        private IEnumerable<Order> GetOrdersForGroup(OrderModel model)
        {
            var list = new Dictionary<int, Order>();
            foreach (var occasion in model.UniqueOrdersFromOccasions.OrderBy(o => o.OrderOccasionId))
            {
                var order = CreateNewOrder();
                model.UpdateOrder(order, occasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), occasion.OccasionEndDateTime.ToDateTimeOffsetSweden(), isGroupOrder: true);
                if (occasion.ExtraInterpreter)
                {
                    if (list.TryGetValue(occasion.ExtraInterpreterFor, out Order parentOrder))
                    {
                        order.IsExtraInterpreterForOrder = parentOrder;
                    }
                }
                else
                {
                    list.Add(occasion.OrderOccasionId.Value, order);
                }
                order.MealBreakIncluded = occasion.MealBreakIncluded;
                yield return order;
            }
        }

        private IEnumerable<OrderOccasionDisplayModel> GetGroupOrders(OrderModel model, PriceListType pricelistType)
        {
            foreach (var occasion in model.UniqueOrdersFromOccasions)
            {
                Order groupOrder = CreateNewOrder();
                // Add list of occasions, with the price information
                model.UpdateOrder(groupOrder, occasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), occasion.OccasionEndDateTime.ToDateTimeOffsetSweden(), isGroupOrder: true);
                occasion.PriceInformationModel = new PriceInformationModel
                {
                    MealBreakIsNotDetucted = occasion.MealBreakIncluded,
                    Header = "Beräknat preliminärt pris",
                    PriceInformationToDisplay = _orderService.GetOrderPriceinformationForConfirmation(groupOrder, pricelistType),
                    UseDisplayHideInfo = true,
                    Description = "Om inget krav eller önskemål om specifik kompetensnivå har angetts i bokningsförfrågan beräknas kostnaden enligt taxan för arvodesnivå Auktoriserad tolk. Slutlig arvodesnivå kan då avvika beroende på vilken tolk som tillsätts enligt principen för kompetensprioritering."
                };
                yield return occasion;
            }
        }

        private OrderGroup CreateNewOrderGroup(List<Order> orders)
        {
            (AspNetUser user, AspNetUser impersonatingUser) = GetUsers();
            return new OrderGroup(user, impersonatingUser, user.CustomerOrganisation, _clock.SwedenNow, orders);
        }

        private Order CreateNewOrder()
        {
            (AspNetUser user, AspNetUser impersonatingUser) = GetUsers();
            return new Order(user, impersonatingUser, user.CustomerOrganisation, _clock.SwedenNow);
        }

        private (AspNetUser, AspNetUser) GetUsers()
        {
            AspNetUser user = _dbContext.Users
               .Include(u => u.CustomerOrganisation)
               .Single(u => u.Id == User.GetUserId());
            var impersonator = User.TryGetImpersonatorId();
            AspNetUser impersonatingUser = null;
            if (impersonator.HasValue)
            {
                impersonatingUser = _dbContext.Users.Single(u => u.Id == impersonator);
            }
            return (user, impersonatingUser);
        }

        private Order GetOrder(int id)
        {
            return _dbContext.Orders
                .Include(o => o.ReplacedByOrder)
                .Include(o => o.ReplacingOrder)
                .Include(o => o.ReplacingOrder).ThenInclude(o => o.CreatedByUser)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Region)
                .Include(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.CustomerUnit)
                .Include(o => o.InterpreterLocations)
                .Include(o => o.CompetenceRequirements)
                .Include(o => o.Group).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment).ThenInclude(at => at.OrderAttachmentHistoryEntries).ThenInclude(oh => oh.OrderChangeLogEntry)
                .Include(o => o.OrderStatusConfirmations).ThenInclude(os => os.ConfirmedByUser)
                .Include(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderChangeConfirmation).ThenInclude(occ => occ.ConfirmedByUser).ThenInclude(cu => cu.Broker)
                .Include(o => o.Attachments).ThenInclude(o => o.Attachment)
                .Include(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderContactPersonHistory).ThenInclude(cph => cph.PreviousContactPersonUser)
                .Include(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.UpdatedByUser)
                .Include(o => o.Requirements).ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions).ThenInclude(r => r.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions).ThenInclude(r => r.ProcessedUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.AnsweringUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.AnswerDisputingUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.TerminatingUser)
                .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.AnsweringUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReceivedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ProcessingUser)
                .Include(o => o.Requests).ThenInclude(r => r.CancelledByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(rr => rr.Ranking).ThenInclude(ra => ra.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(rr => rr.Requisitions).ThenInclude(u => u.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(rr => rr.Complaints).ThenInclude(u => u.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.RequestStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.RequestUpdateLatestAnswerTime).ThenInclude(ru => ru.UpdatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment)
                .Include(o => o.Requests).ThenInclude(r => r.RequestGroup).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment)
                .Include(o => o.Requests).ThenInclude(r => r.Order)
                .Single(o => o.OrderId == id);
        }
    }
}
