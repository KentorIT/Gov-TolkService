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
using Tolk.Web.Enums;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

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
        private readonly ListToModelService _listToModelService;
        private readonly EventLogService _eventLogService;
        private readonly UserService _userService;

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
            CacheService cacheService,
            ListToModelService listToModelService,
            EventLogService eventLogService,
            UserService userService
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
            _listToModelService = listToModelService;
            _eventLogService = eventLogService;
            _userService = userService;
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

            if (order != null && (await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                var request = await _dbContext.Requests.GetActiveRequestByOrderId(id);  
                bool isConnectedToCurrentFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement.IsCurrentAndActiveFrameworkAgreement(request?.Ranking.FrameworkAgreementId);
                var model = OrderViewModel.GetModelFromOrder(order, request, User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator), false, isConnectedToCurrentFrameworkAgreement);              
                model.UserCanEdit = (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded;
                model.UserCanCancelOrder = (await _authorizationService.AuthorizeAsync(User, order, Policies.Cancel)).Succeeded;
                model.UserCanEditContactPerson = (await _authorizationService.AuthorizeAsync(User, order, Policies.EditContact)).Succeeded;
                model.UserCanAccept = (await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded;
                model.UserCanPrint = (await _authorizationService.AuthorizeAsync(User, order, Policies.Print)).Succeeded;

                model.OrderUpdateIsEnabled = _options.EnableOrderUpdate;
                model.TimeIsValidForOrderReplacement = TimeIsValidForOrderReplacement(order.StartAt);
                model.StartAtIsInFuture = order.StartAt > _clock.SwedenNow;

                if (request != null)
                {
                    model.UserCanCreateComplaint = (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded;
                    model.ActiveRequest = RequestViewModel.GetModelFromRequest(request, order.AllowExceedingTravelCost);
                }
                else
                {
                    _logger.LogWarning("Order is missing an active request OrderId: {id}", id);
                    model.ActiveRequest = new RequestViewModel();
                    var frameworkAgreement = await _dbContext.Requests.GetFrameworkByOrderId(id);
                    model.ActiveRequest.FrameworkAgreementNumberOnCreated = frameworkAgreement.AgreementNumber;
                }
                model.ActiveRequest.RegionName = model.RegionName;
                model.ActiveRequest.TimeRange = model.TimeRange;
                model.ActiveRequest.DisplayMealBreakIncluded = model.DisplayMealBreakIncludedText;
                model.ActiveRequest.IsCancelled = model.Status == OrderStatus.CancelledByCreator || model.Status == OrderStatus.CancelledByBroker;

                //LISTS
                model.UseAttachments = CachedUseAttachentSetting(order.CustomerOrganisationId);
                await _listToModelService.AddInformationFromListsToModel(model);
                model.ActiveRequest.RequestCalculatedPriceInformationModel = model.ActiveRequestPriceInformationModel;
                model.ActiveRequest.LanguageAndDialect = model.LanguageAndDialect;
                model.ActiveRequest.AttachmentListModel = model.RequestAttachmentListModel;
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

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Replace(int replacingOrderId, string cancelMessage)
        {

            Order order = await _dbContext.Orders.GetFullOrderById(replacingOrderId);

            if (order != null && (await _authorizationService.AuthorizeAsync(User, order, Policies.Replace)).Succeeded)
            {
                var request = await _dbContext.Requests.GetActiveRequestByOrderId(replacingOrderId);
                if (request.CanCreateReplacementOrderOnCancel &&
                    _cacheService.CurrentOrLatestFrameworkAgreement.IsCurrentAndActiveFrameworkAgreement(request?.Ranking.FrameworkAgreementId) &&
                    TimeIsValidForOrderReplacement(order.StartAt))
                {
                    return View(await _listToModelService.AddInformationFromListsToModel(ReplaceOrderModel.GetModelFromOrder(order, cancelMessage, request.Ranking.Broker.Name, CachedUseAttachentSetting(User.GetCustomerOrganisationId()), _cacheService.CurrentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset)));
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
                Order order = await _dbContext.Orders.GetFullOrderById(model.ReplacingOrderId);
                if (order != null && (await _authorizationService.AuthorizeAsync(User, order, Policies.Replace)).Succeeded)
                {
                    var request = await _dbContext.Requests.GetActiveRequestByOrderId(order.OrderId);
                    if (request.CanCreateReplacementOrderOnCancel &&
                        _cacheService.CurrentOrLatestFrameworkAgreement.IsCurrentAndActiveFrameworkAgreement(request?.Ranking.FrameworkAgreementId) &&
                        TimeIsValidForOrderReplacement(order.StartAt))
                    {
                        using var trn = await _dbContext.Database.BeginTransactionAsync();
                        // add a few lists, used when copying in constructor
                        order.CompetenceRequirements = await _dbContext.OrderCompetenceRequirements.GetOrderedCompetenceRequirementsForOrder(order.OrderId).ToListAsync();
                        order.InterpreterLocations = new List<OrderInterpreterLocation>();
                        Order replacementOrder = new Order(order);
                        model.UpdateOrder(replacementOrder, model.TimeRange.StartDateTime.Value, model.TimeRange.EndDateTime.Value, CachedUseAttachentSetting(User.GetCustomerOrganisationId()));
                        await _orderService.ReplaceOrder(order, replacementOrder, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                        await _dbContext.SaveChangesAsync();
                        trn.Commit();
                        return RedirectToAction("Index", "Home", new { message = "Ersättningsuppdrag är skickat" });
                    }
                }
            }
            return RedirectToAction(nameof(View), new { id = model.ReplacingOrderId, errorMessage = "Det gick inte att skapa ett ersättningsuppdrag för uppdraget, så ingen avbokning kunde heller ske" });
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Update(int id)
        {
            var order = await _dbContext.Orders.GetFullOrderById(id);

            if (order != null && _options.EnableOrderUpdate && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                var selectedInterpreterLocation = (InterpreterLocation)(await _dbContext.Requests.GetActiveRequestByOrderId(id)).InterpreterLocation.Value;
                UpdateOrderModel model = UpdateOrderModel.GetModelFromOrder(order);
                //Competences
                var competenceRequirements = await _dbContext.OrderCompetenceRequirements
                    .GetOrderedCompetenceRequirementsForOrder(id)
                    .Select(r => new { r.CompetenceLevel })
                    .ToListAsync();
                model.OrderRequirements = await OrderRequirementModel.GetFromList(_dbContext.OrderRequirements.GetRequirementsForOrder(id));
                model.RequestedCompetenceLevelFirst = competenceRequirements.FirstOrDefault()?.CompetenceLevel;
                model.RequestedCompetenceLevelSecond = competenceRequirements.Count > 1 ? competenceRequirements[1]?.CompetenceLevel : null;
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.UseAttachments = CachedUseAttachentSetting(User.GetCustomerOrganisationId());
                if (model.UseAttachments)
                {
                    var files = await _dbContext.Attachments.GetAttachmentsForOrderAndGroup(id, order.OrderGroupId)
                        .Select(a => new FileModel
                        {
                            FileName = a.FileName,
                            Id = a.AttachmentId,
                            Size = a.Blob.Length
                        }).ToListAsync();
                    model.Files = files.Any() ? files : null;
                }
                var locations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(model.OrderId).ToListAsync();
                var selectedLocation = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(model.OrderId).SingleAsync(l => l.InterpreterLocation == selectedInterpreterLocation);
                model.RankedInterpreterLocationFirst = locations.Single(l => l.Rank == 1).InterpreterLocation;
                model.SelectedInterpreterLocation = selectedInterpreterLocation;
                if (selectedInterpreterLocation == InterpreterLocation.OffSitePhone || selectedInterpreterLocation == InterpreterLocation.OffSiteVideo)
                {
                    model.OffSiteContactInformation = selectedLocation.OffSiteContactInformation;
                }
                else
                {
                    model.LocationCity = selectedLocation.City;
                    model.LocationStreet = selectedLocation.Street;
                }
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
                var order = await _dbContext.Orders.GetFullOrderById(model.OrderId);
                if (order != null && _options.EnableOrderUpdate && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
                {
                    try
                    {
                        var orderFieldsUpdated = false;
                        var attachmentChanged = false;
                        var contactpersonChanged = false;
                        AspNetUser oldContactPerson = order.ContactPersonUser;
                        IEnumerable<int> oldOrderAttachmentIdsToCompare = await _dbContext.Attachments.GetAttachmentsForOrderAndGroup(model.OrderId, order.OrderGroupId).Select(a => a.AttachmentId).ToListAsync();
                        IEnumerable<int> updatedAttachments = (model.Files?.Any() ?? false) ? model.Files.Select(a => a.Id) : Enumerable.Empty<int>();
                        //check if attachments are changed
                        if (!oldOrderAttachmentIdsToCompare.OrderBy(r => r).SequenceEqual(updatedAttachments.OrderBy(r => r)))
                        {
                            attachmentChanged = true;
                            order.Attachments = await _dbContext.OrderAttachments.GetAttachmentsForOrder(model.OrderId).ToListAsync();
                            if (order.OrderGroupId.HasValue)
                            {
                                order.Group.Attachments = await _dbContext.OrderGroupAttachments.GetAttachmentsForOrderGroup(order.OrderGroupId.Value, order.OrderId).ToListAsync();
                            }
                        }
                        //check if contactperson is changed
                        if (order.ContactPersonId != model.ContactPersonId)
                        {
                            contactpersonChanged = true;
                            _orderService.ChangeContactPerson(order, model.ContactPersonId, User.GetUserId(), User.TryGetImpersonatorId());
                        }
                        //
                        //Note: This retrieves the locations to the order object as well...
                        var locations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(model.OrderId).ToListAsync();
                        var request = await _dbContext.Requests.GetActiveRequestByOrderId(model.OrderId);
                        var selectedInterpreterLocation = (InterpreterLocation)request.InterpreterLocation.Value;
                        //check if something else is updated
                        if (model.IsOrderUpdated(order, locations.Single(l => l.InterpreterLocation == selectedInterpreterLocation)))
                        {
                            orderFieldsUpdated = true;
                        }
                        if (!(orderFieldsUpdated || attachmentChanged || contactpersonChanged))
                        {
                            return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = "OBS! Det fanns inga ändringar att spara på bokningen!" });
                        }
                        if (orderFieldsUpdated || attachmentChanged)
                        {
                            order.OrderChangeLogEntries ??= new List<OrderChangeLogEntry>();
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
                                SelectedInterpreterLocation = selectedInterpreterLocation,
                                Attachments = updatedAttachments,
                                BrokerId = request.Ranking.BrokerId
                            });

                        }
                        await _dbContext.SaveChangesAsync();
                        order = await _dbContext.Orders.GetFullOrderById(model.OrderId);
                        // Note: this discard-fetch will add request to the order entity being tracked by EF
                        _ = await _dbContext.Requests.GetActiveRequestByOrderId(order.OrderId);                        
                        //Note: This retrieves the locations to the order object as well...
                        _ = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(model.OrderId).ToListAsync();                        
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
                        _logger.LogError("Update failed for order {order.OrderId}, message: {errorMessage}.", order.OrderId, ex.Message);
                        return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = ex.Message });
                    }
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.OrderId, errorMessage = "Det gick inte att ändra bokningen." });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetEventLog(int id)
        {
            Order order = await _dbContext.Orders.SingleAsync(o => o.OrderId == id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = (await _eventLogService.GetEventLogForOrder(id)).OrderBy(e => e.Timestamp).ThenBy(e => e.Weight).ToList()
                });
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Create()
        {
            if (!_cacheService.CurrentOrLatestFrameworkAgreement.IsActive)
            {
                return Forbid();
            }
            var now = _clock.SwedenNow.DateTime;
            var firstWorkDay = _dateCalculationService.GetFirstWorkDay(now).Date;
            var panicTime = _dateCalculationService.GetFirstWorkDay(firstWorkDay).Date;
            var nowIsWorkingDay = _dateCalculationService.IsWorkingDay(now);
            if (nowIsWorkingDay && now.Hour >= 14)
            {
                //Add day if after 14 if order creation is a working day...
                panicTime = _dateCalculationService.GetFirstWorkDay(panicTime.AddDays(1).Date).Date;
            }
            DateTime nextPanicTime = nowIsWorkingDay ? _dateCalculationService.GetFirstWorkDay(panicTime.AddDays(1).Date).Date : panicTime;

            var user = await _userService.GetUserWithDefaultSettings(User.GetUserId());
            var currentFrameworkAgreementResponseRuleset = (FrameworkAgreementResponseRuleset)_cacheService.CurrentOrLatestFrameworkAgreement.FrameworkAgreementId;
            var model = new OrderModel()
            {
                LastTimeForRequiringLatestAnswerBy = panicTime.ToSwedishString("yyyy-MM-dd"),
                NextLastTimeForRequiringLatestAnswerBy = nextPanicTime.ToSwedishString("yyyy-MM-dd"),
                CreatedByName = user.FullName,
                UserDefaultSettings = DefaultSettingsModel.GetModel(user, currentFrameworkAgreementResponseRuleset),
                EnableOrderGroups = _options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == User.GetCustomerOrganisationId() && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)),
                UseAttachments = CachedUseAttachentSetting(User.GetCustomerOrganisationId()),
                TravelConditionHours = EnumHelper.GetContractDefinition(currentFrameworkAgreementResponseRuleset).TravelConditionHours,
                TravelConditionKilometers = EnumHelper.GetContractDefinition(currentFrameworkAgreementResponseRuleset).TravelConditionKilometers
            };
            model.UpdateModelWithDefaultSettings(user.CustomerUnits.Where(cu => cu.CustomerUnit.IsActive).Select(cu => cu.CustomerUnitId).ToList());
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Add(OrderModel model)
        {
            if (!_cacheService.CurrentOrLatestFrameworkAgreement.IsActive)
            {
                return Forbid();
            }
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
                using var trn = await _dbContext.Database.BeginTransactionAsync();
                if (model.IsMultipleOrders)
                {
                    var orderGroup = await CreateNewOrderGroup(await GetOrdersForGroup(model));
                    model.UpdateOrderGroup(orderGroup, CachedUseAttachentSetting(User.GetCustomerOrganisationId()));
                    await _dbContext.AddAsync(orderGroup);
                    //TODO: LASTANSWER BY HAS TO BE NULL IF NOT ONLY ONE OCCASION WITH EXTRA INTERPRETER!!
                    await _orderService.CreateRequestGroup(orderGroup, latestAnswerBy: model.LatestAnswerBy);

                    await _dbContext.SaveChangesAsync();
                    trn.Commit();
                    return RedirectToAction(nameof(SentGroup), new { id = orderGroup.OrderGroupId });
                }
                else
                {
                    Order order = await CreateNewOrder();
                    var firstOccasion = model.FirstOccasion;
                    model.UpdateOrder(order, firstOccasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), firstOccasion.OccasionEndDateTime.ToDateTimeOffsetSweden(), useAttachments: CachedUseAttachentSetting(User.GetCustomerOrganisationId()));
                    await _orderService.Create(order, latestAnswerBy: model.LatestAnswerBy);
                    await _dbContext.SaveChangesAsync();
                    trn.Commit();
                    return RedirectToAction(nameof(Sent), new { id = order.OrderId });
                }
            }
            _logger.LogError($"{nameof(Add)} - {nameof(InvalidModelStateErrors)}: {InvalidModelStateErrors}");
            return View(model);
        }

        private string InvalidModelStateErrors => string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<ActionResult> Confirm(OrderModel model)
        {
            var currentFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            if (!currentFrameworkAgreement.IsActive)
            {
                return PartialView("_ErrorMessage", "Det finns inget aktivt ramavtal!");
            }
            try
            {
                Order order = await CreateNewOrder();
                PriceListType pricelistType = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == order.CustomerOrganisation.CustomerOrganisationId).PriceListType;
                OrderViewModel updatedModel = null;
                var firstOccasion = model.FirstOccasion;
                string warningOrderTimeInfo = string.Empty;
                model.UpdateOrder(order, firstOccasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), firstOccasion.OccasionEndDateTime.ToDateTimeOffsetSweden(), useAttachments: CachedUseAttachentSetting(User.GetCustomerOrganisationId()));
                updatedModel = OrderViewModel.GetModelFromOrderForConfirmation(order);
                if (model.IsMultipleOrders)
                {
                    updatedModel.OrderOccasionDisplayModels = await GetGroupOrders(model, pricelistType, currentFrameworkAgreement.BrokerFeeCalculationType);
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
                        PriceInformationToDisplay = _orderService.GetOrderPriceinformationForConfirmation(order, pricelistType, currentFrameworkAgreement.BrokerFeeCalculationType),
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
                updatedModel.UseAttachments = CachedUseAttachentSetting(User.GetCustomerOrganisationId());
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
                updatedModel.CustomerPeppolId = user.CustomerOrganisation.PeppolId;
                updatedModel.CompetenceIsRequired = order.SpecificCompetenceLevelRequired;
                return PartialView(nameof(Confirm), updatedModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in {nameof(Confirm)}", ex);
                return PartialView("_ErrorMessage", "Något gick fel vid sammanställningen av avropet!");
            }
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> SentGroup(int id)
        {
            OrderGroup orderGroup = await _dbContext.OrderGroups.SingleAsync(o => o.OrderGroupId == id);
            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                return View(new OrderGroupSummaryModel
                {
                    OrderGroupNumber = orderGroup.OrderGroupNumber,
                    OrderOccasionDisplayModels = GetModelsForGroupOrders(id)
                });
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Sent(int id)
        {
            Order order = await _dbContext.Orders.SingleAsync(o => o.OrderId == id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                var model = new OrderSentModel
                {
                    OrderNumber = order.OrderNumber,
                    OrderCalculatedPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.OrderPriceRows.GetPriceRowsForOrder(id).ToListAsync(), PriceInformationType.Order, order.MealBreakIncluded ?? false)
                };
                model.OrderCalculatedPriceInformationModel.Header = "Preliminärt pris";
                model.OrderCalculatedPriceInformationModel.CenterHeader = true;

                return View(model);
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Print(int id)
        {
            var order = await _dbContext.Orders.GetFullOrderById(id);

            if (order != null && (await _authorizationService.AuthorizeAsync(User, order, Policies.Print)).Succeeded)
            {
                var request = await _dbContext.Requests.GetActiveRequestIncludeProcessingUserByOrderId(id, false);
                if (!(request?.CanPrint ?? false))
                {
                    return RedirectToAction(nameof(View), new { id, errorMessage = "Bokningen har fel status för att skriva ut en bokningsbekräftelse" });
                }

                var model = OrderViewModel.GetModelFromOrder(order, request, User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator));
                model.BrokerName = request.Ranking.Broker.Name;
                model.CreatedBy = request.Order.CreatedByUser.FullName;
                model.RequestId = request.RequestId;
                model.AnsweredBy = request.AnsweringUser?.FullName;
                model.AnsweredAt = request.AnswerDate;
                model.ExpectedTravelCostInfo = request.ExpectedTravelCostInfo;
                model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                model.InterpreterLocationInfoAnswer = GetInterpreterLocationInfoAnswer(id, (InterpreterLocation)request.InterpreterLocation.Value);
                model.InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel)request.CompetenceLevel;
                model.ActiveRequest = RequestViewModel.GetModelFromRequest(request, order.AllowExceedingTravelCost);

                //Lists

                model.RequestAttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequest(request.RequestId, request.RequestGroupId), "Bifogade filer från förmedling");
                model.ActiveRequest.RequirementAnswers = await RequestRequirementAnswerModel.GetFromList(_dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(request.RequestId));
                model.ActiveRequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync(), PriceInformationType.Request, order.MealBreakIncluded ?? false);
                model.ActiveRequest.RequestCalculatedPriceInformationModel = model.ActiveRequestPriceInformationModel;

                model.ActiveRequest.InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null;
                model.ActiveRequest.Interpreter = request.Interpreter.FullName;
                model.ActiveRequest.InterpreterEmail = request.Interpreter.Email ?? "-";
                model.ActiveRequest.InterpreterPhoneNumber = request.Interpreter.PhoneNumber ?? "-";
                model.ActiveRequest.InterpreterOfficialInterpreterId = request.Interpreter.OfficialInterpreterId ?? "-";
                model.Dialect = GetRequestAnswerDialect(model.Dialect, model.ActiveRequest.RequirementAnswers);
                model.ActiveRequest.AnswerProcessedBy = request.AnswerProcessedBy.HasValue ? request.ProcessingUser.FullName : "Systemet";
                model.ActiveRequest.AnswerProcessedAt = request.AnswerProcessedAt.HasValue ? request.AnswerProcessedAt.Value.ToSwedishString("yyyy-MM-dd HH:mm") : request.AnswerDate.Value.ToSwedishString("yyyy-MM-dd HH:mm");
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Approve(ProcessRequestModel model)
        {
            var request = await _dbContext.Requests.GetRequestById(model.RequestId);

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
            var order = await _dbContext.Orders.GetOrderWithContactsById(model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Cancel)).Succeeded)
            {
                try
                {
                    if (model.AddReplacementOrder)
                    {
                        var request = await _dbContext.Requests.GetActiveRequestByOrderId(order.OrderId);
                        if (request == null)
                        {
                            return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = "Uppdraget kunde inte avbokas" });
                        }
                        if (request.CanCreateReplacementOrderOnCancel &&
                            _cacheService.CurrentOrLatestFrameworkAgreement.IsCurrentAndActiveFrameworkAgreement(request?.Ranking.FrameworkAgreementId) &&
                            TimeIsValidForOrderReplacement(order.StartAt))
                        {
                            //Forward the message to replace
                            return RedirectToAction(nameof(Replace), new { replacingOrderId = model.OrderId, cancelMessage = model.CancelMessage });
                        }
                        else
                        {
                            return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = "Det gick inte att skapa ett ersättningsuppdrag för uppdraget, så ingen avbokning kunde heller ske" });
                        }
                    }
                    await _orderService.CancelOrder(order, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                    await _dbContext.SaveChangesAsync();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("Order {orderId} failed to cancel for user {userId} with error message {errorMessage}.", order.OrderId, User.GetUserId(), ex.Message);
                    return RedirectToAction(nameof(View), new { id = order.OrderId, errorMessage = "Uppdraget kunde inte avbokas" });
                }

                return RedirectToAction(nameof(View), new { id = model.OrderId, Message = "Uppdraget har avbokats" });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmCancellation(int requestId)
        {
            var request = await _dbContext.Requests.GetSimpleRequestById(requestId);

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
            var order = await _dbContext.Orders.SingleAsync(o => o.OrderId == orderId);
            if (order.Status == OrderStatus.NoBrokerAcceptedOrder && (await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                try
                {
                    await _orderService.ConfirmNoAnswer(order, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmNoAnswer failed for order {orderId}, message {errorMessage}.", orderId, ex.Message);
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
            var order = await _dbContext.Orders.SingleAsync(o => o.OrderId == orderId);
            order.OrderStatusConfirmations = await _dbContext.OrderStatusConfirmation.GetStatusConfirmationsForOrder(orderId).ToListAsync();
            if (order.Status == OrderStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                try
                {
                    await _orderService.ConfirmResponeNotAnswered(order, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmResponseNotAnswered failed for order {orderId}, message {errorMessage}.", orderId, ex.Message);
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
            var request = await _dbContext.Requests.GetSimpleRequestById(model.RequestId);

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
            var order = await _dbContext.Orders.GetFullOrderById(model.OrderId);                     
            if (order != null && (await _authorizationService.AuthorizeAsync(User, order, Policies.EditContact)).Succeeded)
            {
                var oldContactPerson = order.ContactPersonUser;
                if (model.ContactPersonId == order.ContactPersonId)
                {
                    return RedirectToAction(nameof(View), new { id = order.OrderId });
                }
                _orderService.ChangeContactPerson(order, model.ContactPersonId, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                // Note: this discard-fetch will add request to the order entity being tracked by EF
                _ = await _dbContext.Requests.GetActiveRequestByOrderId(order.OrderId);
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

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> UpdateExpiry(int orderId, DateTimeOffset latestAnswerBy)
        {
            var order = await _dbContext.Orders.GetOrderWithContactsById(orderId);
            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                try
                {
                    var request = await _dbContext.Requests.GetRequestToUpdateExpiryByOrderId(orderId);
                    if (request == null)
                    {
                        return RedirectToAction("Index", "Home", new { errorMessage = "Denna bokning behöver inte få sista svarstid satt." });
                    }
                    await _orderService.SetRequestExpiryManually(request, latestAnswerBy, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("Order {orderId} user {userId} failed to set last Answer By with error message {errorMessage}.", orderId, User.GetUserId(), ex.Message);
                    return RedirectToAction("Index", "Home", new { errorMessage = "Denna bokning behöver inte få sista svarstid satt." });
                }
                return RedirectToAction("Index", "Home", new { message = $"Sista svarstid för bokning {order.OrderNumber} är satt" });
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        private bool TimeIsValidForOrderReplacement(DateTimeOffset orderStart)
        {
            var noOfDays = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(_clock.SwedenNow.DateTime, orderStart.DateTime);
            return noOfDays > -1 && noOfDays < 2;
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

        private static string GetRequestAnswerDialect(string dialect, List<RequestRequirementAnswerModel> requirementAnswers)
        {
            if (!string.IsNullOrEmpty(dialect) && requirementAnswers != null && requirementAnswers.Any(or => or.RequirementType == RequirementType.Dialect))
            {
                var reqDialect = requirementAnswers.Single(or => or.RequirementType == RequirementType.Dialect);
                return reqDialect.CanMeetRequirement ? $"(dialekt: {reqDialect.Description})" : string.Empty;
            }
            return string.Empty;
        }

        private string GetInterpreterLocationInfoAnswer(int id, InterpreterLocation locationAnswer)
        {
            var location = _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(id).SingleOrDefault(i => i.InterpreterLocation == locationAnswer);
            if (location != null)
            {
                return (location.InterpreterLocation == InterpreterLocation.OffSitePhone || location.InterpreterLocation == InterpreterLocation.OffSiteVideo) ? $"Kontaktinformation: {location.OffSiteContactInformation}" : $"Adress: {location.FullAddress}";
            }
            return string.Empty;
        }

        private async Task<List<Order>> GetOrdersForGroup(OrderModel model)
        {
            var orders = new List<Order>();
            var list = new Dictionary<int, Order>();
            foreach (var occasion in model.UniqueOrdersFromOccasions.OrderBy(o => o.OrderOccasionId))
            {
                var order = await CreateNewOrder();
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
                order.MealBreakIncluded = occasion.MealBreakIncluded ?? false && ((int)(occasion.OccasionEndDateTime - occasion.OccasionStartDateTime).TotalMinutes > 300);
                orders.Add(order);
            }
            return orders;
        }

        private async Task<IEnumerable<OrderOccasionDisplayModel>> GetGroupOrders(OrderModel model, PriceListType pricelistType, BrokerFeeCalculationType brokerFeeCalculationType)
        {
            var occasions = new List<OrderOccasionDisplayModel>();
            foreach (var occasion in model.UniqueOrdersFromOccasions)
            {
                Order groupOrder = await CreateNewOrder();
                // Add list of occasions, with the price information
                model.UpdateOrder(groupOrder, occasion.OccasionStartDateTime.ToDateTimeOffsetSweden(), occasion.OccasionEndDateTime.ToDateTimeOffsetSweden(), isGroupOrder: true);
                occasion.PriceInformationModel = new PriceInformationModel
                {
                    MealBreakIsNotDetucted = occasion.MealBreakIncluded ?? false,
                    Header = "Beräknat preliminärt pris",
                    PriceInformationToDisplay = _orderService.GetOrderPriceinformationForConfirmation(groupOrder, pricelistType, brokerFeeCalculationType),
                    UseDisplayHideInfo = true,
                    Description = "Om inget krav eller önskemål om specifik kompetensnivå har angetts i bokningsförfrågan beräknas kostnaden enligt taxan för arvodesnivå Auktoriserad tolk. Slutlig arvodesnivå kan då avvika beroende på vilken tolk som tillsätts enligt principen för kompetensprioritering."
                };
                occasions.Add(occasion);
            }
            return occasions;
        }

        private bool CachedUseAttachentSetting(int customerOrganisationId) => _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && !c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.HideAttachmentField));

        private async Task<OrderGroup> CreateNewOrderGroup(List<Order> orders)
        {
            (AspNetUser user, AspNetUser impersonatingUser) = await GetUsers();
            return new OrderGroup(user, impersonatingUser, user.CustomerOrganisation, _clock.SwedenNow, orders);
        }

        private async Task<Order> CreateNewOrder()
        {
            (AspNetUser user, AspNetUser impersonatingUser) = await GetUsers();
            return new Order(user, impersonatingUser, user.CustomerOrganisation, _clock.SwedenNow);
        }

        private async Task<(AspNetUser, AspNetUser)> GetUsers()
        {
            AspNetUser user = await _dbContext.Users.GetUserWithCustomerOrganisationById(User.GetUserId());
            var impersonator = User.TryGetImpersonatorId();
            AspNetUser impersonatingUser = null;
            if (impersonator.HasValue)
            {
                impersonatingUser = await _dbContext.Users.GetUserWithCustomerOrganisationById(impersonator.Value);
            }
            return (user, impersonatingUser);
        }

        private IEnumerable<OrderOccasionDisplayModel> GetModelsForGroupOrders(int id)
        {
            var list = new List<OrderOccasionDisplayModel>();
            foreach (var order in _dbContext.Orders.Where(o => o.OrderGroupId == id).ToList())
            {
                list.Add(OrderOccasionDisplayModel.GetModelFromOrder(order,
                    PriceInformationModel.GetPriceinformationToDisplay(_dbContext.OrderPriceRows.GetPriceRowsForOrder(order.OrderId).ToList(), PriceInformationType.Order, initialCollapse: false, mealBreakIncluded: order.MealBreakIncluded ?? false))
                );
            }
            return list;
        }

    }
}
