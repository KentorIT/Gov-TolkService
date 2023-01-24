using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic;
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
    public class HomeController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<HomeController> _logger;
        private readonly VerificationService _verificationService;
        private readonly TolkOptions _options;
        private readonly CacheService _cacheService;

        public HomeController(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            ILogger<HomeController> logger,
            VerificationService verificationService,
            IOptions<TolkOptions> options,
            CacheService cacheService
            )
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _clock = clock;
            _authorizationService = authorizationService;
            _logger = logger;
            _verificationService = verificationService;
            _options = options?.Value;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index(string message, string errorMessage)
        {
            if (!_dbContext.IsUserStoreInitialized)
            {
                return RedirectToAction("CreateInitialUser", "Account");
            }

            if (!User.Identity.IsAuthenticated)
            {
                return View("IndexNotLoggedIn");
            }
            if (!User.IsImpersonated())
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (!await _userManager.HasPasswordAsync(user))
                    {
                        return RedirectToAction("RegisterNewAccount", "Account");
                    }
                    if (!User.HasClaim(c => c.Type == TolkClaimTypes.PersonalName))
                    {
                        return RedirectToAction("Edit", "Account");
                    }
                }
            }
            return await AuthenticatedUser(message, errorMessage);
        }

        [ValidateAntiForgeryToken]
        private async Task<IActionResult> AuthenticatedUser(string message, string errorMessage)
        {
            return View(new StartViewModel
            {
                PageTitle = (User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator)) ? $"Startsida för {Constants.SystemName}" : "Aktiva bokningar",
                Message = message,
                ErrorMessage = errorMessage,
                ConfirmationMessages = GetConfirmationMessages(),
                SystemMessages = await GetSystemMessagesForUser(),
                StartLists = await GetStartLists(),
                IsBroker = User.TryGetBrokerId().HasValue,
                IsCustomer = User.TryGetCustomerOrganisationId().HasValue
            });
        }

        private async Task<IEnumerable<SystemMessage>> GetSystemMessagesForUser()
        {
            bool displayApplicationAdministratorMessages = User.IsInRole(Roles.ApplicationAdministrator);
            bool displaySystemAdministratorMessages = User.IsInRole(Roles.SystemAdministrator);
            bool displayBrokerMessages = displaySystemAdministratorMessages || User.TryGetBrokerId().HasValue;
            bool displayCustomerMessages = displaySystemAdministratorMessages || User.TryGetCustomerOrganisationId().HasValue;
            bool displayCentralAdministratorMessages = displaySystemAdministratorMessages || User.IsInRole(Roles.CentralAdministrator);

            var messages = _dbContext.SystemMessages
                .Where(s => s.ActiveFrom < _clock.SwedenNow
                && s.ActiveTo.Date >= _clock.SwedenNow.Date
                && (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.All
                || (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.BrokerUsers && displayBrokerMessages)
                || (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.CustomerUsers && displayCustomerMessages)
                || (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.CentralAdministrators && displayCentralAdministratorMessages)))
                .ToList();
            if (displayApplicationAdministratorMessages)
            {
                var statusMessages = await _verificationService.VerifySystemStatus();
                if (!statusMessages.Success)
                {
                    messages = messages.Union(statusMessages.Items.Where(i => !i.Success).Select(i => new SystemMessage
                    {
                        CreatedAt = DateTimeOffset.UtcNow,
                        SystemMessageType = SystemMessageType.Warning,
                        SystemMessageHeader = "Statusvarning",
                        SystemMessageText = $"Systemtestet \"{i.Test}\" fungerade inte. Se dokumentation för mer information!"
                    })).ToList();
                }
            }
            return messages.OrderByDescending(s => s.SystemMessageType)
                .ThenByDescending(s => s.LastUpdatedCreatedAt);
        }

        private async Task<IEnumerable<StartList>> GetStartLists()
        {
            var result = Enumerable.Empty<StartList>();

            if ((await _authorizationService.AuthorizeAsync(User, Policies.Customer)).Succeeded)
            {
                result = result.Union(GetCustomerStartLists());
            }
            if ((await _authorizationService.AuthorizeAsync(User, Policies.Broker)).Succeeded)
            {
                result = result.Union(GetBrokerStartLists());
            }
            if ((await _authorizationService.AuthorizeAsync(User, Policies.Interpreter)).Succeeded)
            {
                result = result.Union(GetInterpreterStartLists());
            }
            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<JsonResult> StartListWithActionColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<ActionStartListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(ActionStartListItemModel.Customer)).Visible = !(await _authorizationService.AuthorizeAsync(User, Policies.Customer)).Succeeded;
            return Json(definition);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<JsonResult> StartListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<StartListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(StartListItemModel.Customer)).Visible = !(await _authorizationService.AuthorizeAsync(User, Policies.Customer)).Succeeded;
            return Json(definition);
        }

        #region Customer lists

        private IEnumerable<StartList> GetCustomerStartLists()
        {
            yield return new StartList
            {
                HeaderClass = "start-list-customer-action-header",
                HeaderLoading = "Kräver handling av myndighet (laddar...)",
                Header = "Kräver handling av myndighet (_TOTAL_ st)",
                EmptyHeader = "Kräver handling av myndighet",
                EmptyMessage = "För tillfället finns det inga aktiva bokningar som kräver handling av myndigheten",
                HasReviewAction = true,
                TableDataPath = new ActionDefinition { Controller = "Home", Action = nameof(ListCustomerActionItems) },
                TableColumnDefinitionPath = new ActionDefinition { Controller = "Home", Action = nameof(StartListWithActionColumnDefinition) },
                DefaultLinkPath = new ActionDefinition { Controller = "Order", Action = "View" }
            };
            yield return new StartList
            {
                HeaderClass = "start-list-customer-waiting-header",
                HeaderLoading = "Skickade bokningar (laddar...)",
                Header = "Skickade bokningar (_TOTAL_ st)",
                EmptyHeader = "Skickade bokningar",
                EmptyMessage = "För tillfället finns det inga aktiva bokningsförfrågningar som är skickade",
                TableDataPath = new ActionDefinition { Controller = "Home", Action = nameof(ListCustomerWaitingItems) },
                TableColumnDefinitionPath = new ActionDefinition { Controller = "Home", Action = nameof(StartListColumnDefinition) },
                DefaultLinkPath = new ActionDefinition { Controller = "Order", Action = "View" }
            };

            yield return new StartList
            {
                HeaderClass = "start-list-customer-approved-header",
                HeaderLoading = "Tillsatta bokningar (laddar...)",
                Header = "Tillsatta bokningar (_TOTAL_ st)",
                EmptyHeader = "Tillsatta bokningar",
                EmptyMessage = "För tillfället finns det inga aktiva bokningar som är tillsatta",
                TableDataPath = new ActionDefinition { Controller = "Home", Action = nameof(ListCustomerApprovedItems) },
                TableColumnDefinitionPath = new ActionDefinition { Controller = "Home", Action = nameof(StartListColumnDefinition) },
                DefaultLinkPath = new ActionDefinition { Controller = "Order", Action = "View" }
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ListCustomerActionItems(IDataTablesRequest request)
        {
            var entities = CustomerActionListItems(User.GetCustomerOrganisationId(), User.GetUserId(), User.TryGetAllCustomerUnits());
            return AjaxDataTableHelper.GetData(request, entities.Count, entities.AsQueryable(), d => d, s => s.OrderByDescending(l => l.OrderNumber));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ListCustomerWaitingItems(IDataTablesRequest request)
        {
            var entities = CustomerWaitingListItems(User.GetCustomerOrganisationId(), User.GetUserId(), User.TryGetAllCustomerUnits());
            return AjaxDataTableHelper.GetData(request, entities.Count, entities.AsQueryable(), d => d, s => s.OrderByDescending(l => l.OrderNumber));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ListCustomerApprovedItems(IDataTablesRequest request)
        {
            var entities = CustomerApprovedListItems(User.GetCustomerOrganisationId(), User.GetUserId(), User.TryGetAllCustomerUnits());
            return AjaxDataTableHelper.GetData(request, entities.Count, entities.AsQueryable(), d => d, s => s.OrderByDescending(l => l.OrderNumber));
        }

        private List<ActionStartListItemModel> CustomerActionListItems(int customerOrganisationId, int userId, IEnumerable<int> customerUnits)
        {
            var actionList = new List<ActionStartListItemModel>();
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits)
                    .Where(o => o.RowType == StartListRowType.Order && (o.OrderStatus == OrderStatus.RequestRespondedAwaitingApproval || o.OrderStatus == OrderStatus.AwaitingDeadlineFromCustomer)).ToList()
                    .Select(o => new ActionStartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        EntityId = o.OrderId,
                        InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        LanguageName = o.LanguageName,
                        OrderNumber = o.OrderNumber,
                        Status = GetStartListStatusForCustomer(o.OrderStatus, o.ReplacingOrderId ?? 0),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && o.LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)o.LatestAnswerTimeForCustomer.Value.DateTime : null,
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted orders to approve, orders awaiting deadline in method {nameof(CustomerActionListItems)}");
            }
            //Orders to approve where interpreter is changed
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.RequestRespondedNewInterpreter).ToList()
                    .Select(o => new ActionStartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        EntityId = o.OrderId,
                        InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        LanguageName = o.LanguageName,
                        OrderNumber = o.OrderNumber,
                        Status = GetStartListStatusForCustomer(o.OrderStatus, o.ReplacingOrderId ?? 0),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && o.LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)o.LatestAnswerTimeForCustomer.Value.DateTime : null,
                        OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders to approve where interpreter is changed in method {nameof(CustomerActionListItems)}");
            }
            //Accepted ordergroups to approve 
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)))
                {
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.RequestRespondedAwaitingApproval).ToList()
                    .Select(og => new ActionStartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                        EntityId = og.OrderGroupId.Value,
                        InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)og.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ExtraCompetenceLevel = (CompetenceAndSpecialistLevel?)og.ExtraCompetencelevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        LinkOverride = "/OrderGroup/View",
                        LanguageName = og.LanguageName,
                        OrderNumber = og.OrderGroupNumber,
                        Status = GetStartListStatusForCustomer((OrderStatus)og.OrderGroupStatus, 0, true),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && og.LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)og.LatestAnswerTimeForCustomer.Value.DateTime : null,
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                    //Order groups awaiting deadline from customer (customer should set last answer date)
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.AwaitingDeadlineFromCustomer).ToList()
                    .Select(og => new ActionStartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                        EntityId = og.OrderGroupId.Value,
                        InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        LinkOverride = "/OrderGroup/View",
                        LanguageName = og.LanguageName,
                        OrderNumber = og.OrderGroupNumber,
                        Status = GetStartListStatusForCustomer((OrderStatus)og.OrderGroupStatus, 0, true),
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted ordergroups to approve in method {nameof(CustomerActionListItems)}");
            }
            try
            {
                if (_options.AllowDeclineExtraInterpreterOnRequestGroups)
                {
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.RequestAwaitingPartialAccept).ToList()
                        .Select(og => new ActionStartListItemModel
                        {
                            OrderDateTimeRange = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                            EntityId = og.OrderGroupId.Value,
                            InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                            CompetenceLevel = (CompetenceAndSpecialistLevel?)og.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,//this might not be correct
                            LinkOverride = "/OrderGroup/View",
                            LanguageName = og.LanguageName,
                            OrderNumber = og.OrderGroupNumber,
                            Status = GetStartListStatusForCustomer((OrderStatus)og.OrderGroupStatus, 0, true),
                            IsSingleOccasion = og.IsSingleOccasion,
                            HasExtraInterpreter = og.HasExtraInterpreter,
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for RequestAwaitingPartialAccept in method {nameof(CustomerActionListItems)}");
            }
            //Orders not answered by creator, no broker accepted order (must include groups since change of interpreter can handle LatestAnswerTimeForCustomer)
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Where(o => o.RowType == StartListRowType.Order && (((!o.OrderGroupId.HasValue || o.OrderGroupStatus != OrderStatus.NoBrokerAcceptedOrder) && o.OrderStatus == OrderStatus.NoBrokerAcceptedOrder) ||
                            (_options.EnableSetLatestAnswerTimeForCustomer && (!o.OrderGroupId.HasValue || o.OrderGroupStatus != OrderStatus.ResponseNotAnsweredByCreator) && o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator))).ToList()
                        .Select(o => new ActionStartListItemModel
                        {
                            OrderDateTimeRange = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                            EntityId = o.OrderId,
                            InfoDate = (o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator) ? o.LatestAnswerTimeForCustomer?.DateTime ?? o.StartAt.DateTime : GetInfoDateForCustomer(o)?.DateTime,
                            CompetenceLevel = o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator ? (CompetenceAndSpecialistLevel?)o.CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter,
                            LanguageName = o.LanguageName,
                            OrderNumber = o.OrderNumber,
                            Status = o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator ? StartListItemStatus.RespondedRequestNotAnswered : o.ReplacingOrderId != null ? StartListItemStatus.ReplacementOrderNotAnswered : StartListItemStatus.OrderNotAnswered,
                            OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                        }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders not answered by creator, no broker accepted order in method {nameof(CustomerActionListItems)}");
            }
            //Ordergroups not answered by creator, no broker accepted order
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)))
                {
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Where(og => og.RowType == StartListRowType.OrderGroup && ((og.OrderGroupStatus == OrderStatus.NoBrokerAcceptedOrder)
                            || (_options.EnableSetLatestAnswerTimeForCustomer && og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator))).ToList()
                        .Select(og => new ActionStartListItemModel
                        {
                            OrderDateTimeRange = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                            EntityId = og.OrderGroupId.Value,
                            InfoDate = og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator ? og.LatestAnswerTimeForCustomer?.DateTime ?? og.StartAt.DateTime : GetInfoDateForCustomer(og)?.DateTime,
                            CompetenceLevel = og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator ? (CompetenceAndSpecialistLevel?)og.CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter,
                            LinkOverride = "/OrderGroup/View",
                            LanguageName = og.LanguageName,
                            OrderNumber = og.OrderGroupNumber,
                            Status = og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator ? StartListItemStatus.RespondedRequestGroupNotAnswered : StartListItemStatus.OrderGroupNotAnswered,
                            IsSingleOccasion = og.IsSingleOccasion,
                            HasExtraInterpreter = og.HasExtraInterpreter,
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Ordergroups not answered by creator, no broker accepted order in method {nameof(CustomerActionListItems)}");
            }
            //Cancelled by broker
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.CancelledByBroker).ToList()
                .Select(o => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                    EntityId = o.OrderId,
                    InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel,
                    LanguageName = o.LanguageName,
                    OrderNumber = o.OrderNumber,
                    Status = StartListItemStatus.OrderCancelled,
                    OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders cancelled by broker in method {nameof(CustomerActionListItems)}");
            }
            //Requisitions to process
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeContact: true, includeOrderGroupOrders: true)
                    .Where(r => r.RowType == StartListRowType.Requisition)
                    .Select(r => new ActionStartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                        EntityId = r.OrderId,
                        InfoDate = r.EntityDate.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        LinkOverride = "/Order/View/?tab=requisition",
                        LanguageName = r.LanguageName,
                        OrderNumber = r.OrderNumber,
                        Status = StartListItemStatus.RequisitonArrived,
                        OrderGroupNumber = r.OrderGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requisitions to review in method {nameof(CustomerActionListItems)}");
            }
            //Disputed complaints
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeContact: true, includeOrderGroupOrders: true)
                    .Where(c => c.RowType == StartListRowType.Complaint)
                    .Select(c => new ActionStartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = c.StartAt, EndDateTime = c.EndAt },
                        EntityId = c.OrderId,
                        InfoDate = c.AnsweredAt.HasValue ? c.AnsweredAt.Value.DateTime : c.EntityDate.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)c.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        LinkOverride = "/Order/View/?tab=complaint",
                        LanguageName = c.LanguageName,
                        OrderNumber = c.OrderNumber,
                        Status = StartListItemStatus.ComplaintEvent,
                        OrderGroupNumber = c.OrderGroupId.HasValue ? $"Del av {c.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Disputed complaints in method {nameof(CustomerActionListItems)}");
            }

            return actionList;
        }

        private List<StartListItemModel> CustomerWaitingListItems(int customerOrganisationId, int userId, IEnumerable<int> customerUnits)
        {
            var  sentOrders = new List<StartListItemModel>();
            //Sent orders
            try
            {
                sentOrders = _dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, false, false)
                    .Where(o => o.RowType == StartListRowType.Order && (o.OrderStatus == OrderStatus.Requested || o.OrderStatus == OrderStatus.RequestAcceptedAwaitingInterpreter)
                    && o.EndAt > _clock.SwedenNow)
                    .Select(o => new StartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        EntityId = o.OrderId,
                        InfoDate = o.EntityDate.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        LanguageName = o.LanguageName,
                        OrderNumber = o.OrderNumber,
                        RequestAcceptedAt = o.AcceptedAt.HasValue ? (DateTime?)o.AcceptedAt.Value.DateTime : null,
                        Status = o.ReplacingOrderId.HasValue ? StartListItemStatus.ReplacementOrderCreated : (o.OrderStatus == OrderStatus.Requested ? StartListItemStatus.OrderCreated : StartListItemStatus.OrderAccepted)
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Sent orders in method {nameof(CustomerWaitingListItems)}");
            }
            //Sent ordergroups
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)))
                {
                    sentOrders.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(og => og.RowType == StartListRowType.OrderGroup && (og.OrderStatus == OrderStatus.Requested || og.OrderStatus == OrderStatus.RequestAcceptedAwaitingInterpreter) && og.EndAt > _clock.SwedenNow).ToList()
                    .Select(og => new StartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                        EntityId = og.OrderGroupId.Value,
                        InfoDate = og.EntityDate.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        LinkOverride = "/OrderGroup/View",
                        LanguageName = og.LanguageName,
                        OrderNumber = og.OrderGroupNumber,
                        RequestAcceptedAt = og.AcceptedAt.HasValue ? (DateTime?)og.AcceptedAt.Value.DateTime : null,
                        Status = og.OrderStatus == OrderStatus.Requested ? StartListItemStatus.OrderGroupCreated : StartListItemStatus.OrderGroupAccepted,
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Sent ordergroups in method {nameof(CustomerWaitingListItems)}");
            }

            return sentOrders;
        }

        private List<StartListItemModel> CustomerApprovedListItems(int customerOrganisationId, int userId, IEnumerable<int> customerUnits)
        {
            //Approved orders
            List<StartListItemModel> approvedOrders = new List<StartListItemModel>();
            try
            {
                approvedOrders = _dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, false, true)
                    .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.ResponseAccepted && o.EndAt > _clock.SwedenNow)
                .Select(o => new StartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                    EntityId = o.OrderId,
                    InfoDate = o.AnsweredAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel)o.CompetenceLevel,
                    LanguageName = o.LanguageName,
                    OrderNumber = o.OrderNumber,
                    Status = StartListItemStatus.OrderApproved,
                    OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Approved orders in method {nameof(CustomerApprovedListItems)}");
            }

            return approvedOrders;
        }

        private static StartListItemStatus GetStartListStatusForCustomer(OrderStatus status, int replacingOrderId, bool isGroup = false)
        {
            return status == OrderStatus.CancelledByBroker ? StartListItemStatus.OrderCancelled
                : (status == OrderStatus.NoBrokerAcceptedOrder && replacingOrderId > 0) ? StartListItemStatus.ReplacementOrderNotAnswered
                : (status == OrderStatus.NoBrokerAcceptedOrder && replacingOrderId == 0) ? isGroup ? StartListItemStatus.OrderGroupNotAnswered : StartListItemStatus.OrderNotAnswered
                : status == OrderStatus.RequestRespondedNewInterpreter ? StartListItemStatus.NewInterpreterForApproval
                : status == OrderStatus.AwaitingDeadlineFromCustomer ? StartListItemStatus.AwaitingDeadlineFromCustomer
                : status == OrderStatus.RequestAwaitingPartialAccept ? StartListItemStatus.PartialGroupResponseAwaitingApproval
                : isGroup ? StartListItemStatus.OrderGroupAwaitingApproval : StartListItemStatus.OrderAcceptedForApproval;
        }

        private static DateTimeOffset? GetInfoDateForCustomer(CustomerStartListRow c)
        {
            return c.OrderStatus == OrderStatus.CancelledByBroker ? c.CancelledAt
                //if status is NoBrokerAcceptedOrder take answerDate (denied/declined) else take expiresAt (no answer)
                : c.OrderStatus == OrderStatus.NoBrokerAcceptedOrder ? c.AnsweredAt ?? c.RequestExpiresAt
                : c.OrderStatus == OrderStatus.AwaitingDeadlineFromCustomer ? c.LastRequestCreatedUpdatedAt
                : c.AnsweredAt;
        }

        #endregion

        #region Broker lists

        private IEnumerable<StartList> GetBrokerStartLists()
        {
            yield return new StartList
            {
                HeaderClass= "start-list-action-header",
                HeaderLoading = "Kräver handling av förmedling (laddar...)",
                Header = "Kräver handling av förmedling (_TOTAL_ st)",
                EmptyHeader = "Kräver handling av förmedling",
                EmptyMessage = "För tillfället finns det inga aktiva bokningar som kräver handling av förmedling",
                HasReviewAction = true,
                TableDataPath = new ActionDefinition {Controller = "Home", Action = nameof(ListBrokerActionItems) },
                TableColumnDefinitionPath = new ActionDefinition { Controller = "Home", Action = nameof(StartListWithActionColumnDefinition) },
                DefaultLinkPath = new ActionDefinition { Controller = "Request", Action = "View" }
            };

            yield return new StartList
            {
                HeaderClass = "start-list-waiting-header",
                HeaderLoading = "Tillsatta bokningar som inväntar godkännande (laddar...)",
                Header = $"Tillsatta bokningar som inväntar godkännande (_TOTAL_ st)",
                EmptyHeader = $"Tillsatta bokningar som inväntar godkännande",
                EmptyMessage = "För tillfället finns det inga tillsatta bokningar som inväntar godkännande",
                TableDataPath = new ActionDefinition { Controller = "Home", Action = nameof(ListBrokerWaitingItems) },
                TableColumnDefinitionPath = new ActionDefinition { Controller = "Home", Action = nameof(StartListColumnDefinition) },
                DefaultLinkPath = new ActionDefinition { Controller = "Request", Action = "View" }
            };

            yield return new StartList
            {
                HeaderClass = "start-list-approved-header",
                HeaderLoading = "Tillsatta bokningar (laddar...)",
                Header = "Tillsatta bokningar (_TOTAL_ st)",
                EmptyHeader = "Tillsatta bokningar",
                EmptyMessage = "För tillfället finns det inga aktiva bokningar som är tillsatta",
                TableDataPath = new ActionDefinition { Controller = "Home", Action = nameof(ListBrokerApprovedItems) },
                TableColumnDefinitionPath = new ActionDefinition { Controller = "Home", Action = nameof(StartListColumnDefinition) },
                DefaultLinkPath = new ActionDefinition { Controller = "Request", Action = "View" }
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ListBrokerActionItems(IDataTablesRequest request)
        {
            var entities = BrokerActionListItems(User.GetBrokerId(), User.GetUserId());
            return AjaxDataTableHelper.GetData(request, entities.Count, entities.AsQueryable(), d => d, s => s.OrderBy(l => l.LatestDate == null).ThenBy(l => l.RequestAcceptAt == null ? l.LatestDate : l.RequestAcceptAt)); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ListBrokerWaitingItems(IDataTablesRequest request)
        {
            var entities = BrokerWaitingListItems(User.GetBrokerId(), User.GetUserId());
            return AjaxDataTableHelper.GetData(request, entities.Count, entities.AsQueryable(), d => d, s => s.OrderByDescending(l => l.OrderNumber));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ListBrokerApprovedItems(IDataTablesRequest request)
        {
            var entities = BrokerApprovedListItems(User.GetBrokerId(), User.GetUserId());
            return AjaxDataTableHelper.GetData(request, entities.Count, entities.AsQueryable(), d => d, s => s.OrderByDescending(l => l.OrderNumber));
        }

        private List<ActionStartListItemModel> BrokerActionListItems(int brokerId, int userId)
        {
            var actionList = new List<ActionStartListItemModel>();
            //Requests awaiting full answer
            try
            {
                var list = EnumHelper.GetEnumsWithParent<RequestAnswerRuleType, RequiredAnswerLevel>(RequiredAnswerLevel.Full);
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RequestGroupId == null &&
                    list.Contains(r.RequestAnswerRuleType.Value) &&
                    r.RowType == StartListRowType.Request &&
                    (r.RequestStatus == RequestStatus.Created || r.RequestStatus == RequestStatus.Received))
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = r.LastRequestCreatedUpdatedAt.HasValue ? (DateTime?)r.LastRequestCreatedUpdatedAt.Value.DateTime : GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    LatestDate = r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null,
                    ViewedByUser = GetViewedByUserName(r, userId)
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests awaiting full answer in method {nameof(BrokerActionListItems)}");
            }
            //Requests awaiting acceptance
            try
            {
                var list = EnumHelper.GetEnumsWithParent<RequestAnswerRuleType, RequiredAnswerLevel>(RequiredAnswerLevel.Acceptance);
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RequestGroupId == null &&
                    list.Contains(r.RequestAnswerRuleType.Value) &&
                    r.RowType == StartListRowType.Request &&
                    (r.RequestStatus == RequestStatus.Created || r.RequestStatus == RequestStatus.Received))
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = r.LastRequestCreatedUpdatedAt.HasValue ? (DateTime?)r.LastRequestCreatedUpdatedAt.Value.DateTime : GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    LatestDate = r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null,
                    RequestAcceptAt = r.LastAcceptAt.HasValue ? (DateTime?)r.LastAcceptAt.Value.DateTime : null,
                    ViewedByUser = GetViewedByUserName(r, userId)
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests awaiting full answer in method {nameof(BrokerActionListItems)}");
            }
            //Accepted Requests awaiting answer
            try
            {
                var list = EnumHelper.GetEnumsWithParent<RequestAnswerRuleType, RequiredAnswerLevel>(RequiredAnswerLevel.Acceptance);
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RequestGroupId == null &&
                    list.Contains(r.RequestAnswerRuleType.Value) &&
                    r.RowType == StartListRowType.Request &&
                    (r.RequestStatus == RequestStatus.AcceptedAwaitingInterpreter))
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = r.LastRequestCreatedUpdatedAt.HasValue ? (DateTime?)r.LastRequestCreatedUpdatedAt.Value.DateTime : GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    LatestDate = r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null,
                    RequestAcceptedAt = r.AcceptedAt.HasValue ? (DateTime?)r.AcceptedAt.Value.DateTime : null,
                    ViewedByUser = GetViewedByUserName(r, userId)
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests awaiting full answer in method {nameof(BrokerActionListItems)}");
            }
            //Denied requests 
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroupStatus != RequestStatus.DeniedByCreator)) &&
                    r.RequestStatus == RequestStatus.DeniedByCreator)
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Denied requests in method {nameof(BrokerActionListItems)}");
            }
            //Requests with status CancelledByCreatorWhenApproved
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved)
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    LatestDate = (r.RequestStatus == RequestStatus.Created || r.RequestStatus == RequestStatus.Received) ? (r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null) : null,
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests with status CancelledByCreatorWhenApproved in method {nameof(BrokerActionListItems)}");
            }
            //Non answered responded requests 
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroupStatus != RequestStatus.ResponseNotAnsweredByCreator)) && r.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator)
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = r.LatestAnswerTimeForCustomer.HasValue ? r.LatestAnswerTimeForCustomer.Value.DateTime : r.StartAt.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    LatestDate = r.RequestIsToBeProcessedByBroker ? (r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null) : null,
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Non answered responded requests in method {nameof(BrokerActionListItems)}");
            }
            //Non confirmed order changes
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && r.EndAt > _clock.SwedenNow && (r.RequestStatus == RequestStatus.Approved || r.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed) && r.OrderChangedAt.HasValue)
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    InfoDate = r.OrderChangedAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = StartListItemStatus.OrderChanged,
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Non confirmed order changes in method {nameof(BrokerActionListItems)}");
            }
            //Requestgroups status received, created, denied, not answered by customer
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(rg => rg.RowType == StartListRowType.RequestGroup && (rg.RequestGroupStatus == RequestStatus.Created || rg.RequestGroupStatus == RequestStatus.Received || rg.RequestGroupStatus == RequestStatus.DeniedByCreator || rg.RequestGroupStatus == RequestStatus.ResponseNotAnsweredByCreator || rg.RequestGroupStatus == RequestStatus.AcceptedAwaitingInterpreter))
                .Select(rg => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = rg.StartAt, EndDateTime = rg.EndAt },
                    EntityId = (int)rg.RequestGroupId,
                    InfoDate = GetInfoDateForGroupForBroker(rg),
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)rg.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    ExtraCompetenceLevel = (CompetenceAndSpecialistLevel?)rg.ExtraCompetencelevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = rg.CustomerName,
                    LanguageName = rg.LanguageName,
                    OrderNumber = rg.OrderGroupNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)rg.RequestGroupStatus, 0, true),
                    LatestDate = rg.RequestGroupIsToBeProcessedByBroker ? (rg.RequestExpiresAt.HasValue ? (DateTime?)rg.RequestExpiresAt.Value.DateTime : null) : null,
                    ViewedByUser = GetViewedByUserName(rg, userId),
                    RequestAcceptedAt = rg.AcceptedAt.HasValue ? (DateTime?)rg.AcceptedAt.Value.DateTime : null,
                    LinkOverride = $"/RequestGroup/View",
                    IsSingleOccasion = rg.IsSingleOccasion,
                    HasExtraInterpreter = rg.HasExtraInterpreter
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requestgroups status received, created, denied, not answered by customer in method {nameof(BrokerActionListItems)}");
            }
            //Complaints
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(c => c.ComplaintStatus == ComplaintStatus.Created)
                .Select(c => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = c.StartAt, EndDateTime = c.EndAt },
                    EntityId = (int)c.RequestId,
                    LinkOverride = $"/Request/View/?tab=complaint",
                    InfoDate = c.EntityDate.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)c.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = c.CustomerName,
                    LanguageName = c.LanguageName,
                    OrderNumber = c.OrderNumber,
                    Status = StartListItemStatus.ComplaintEvent,
                    ViewedByUser = GetViewedByUserName(c, userId),
                    OrderGroupNumber = c.RequestGroupId.HasValue ? $"Del av {c.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Complaints in method {nameof(BrokerActionListItems)}");
            }
            //Requests to be reported
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.Approved && r.StartAt < _clock.SwedenNow)
                 .Select(r => new ActionStartListItemModel
                 {
                     OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                     EntityId = (int)r.RequestId,
                     InfoDate = r.EndAt.DateTime,
                     InfoDateDescription = "Utfört: ",
                     CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                     CustomerName = r.CustomerName,
                     LanguageName = r.LanguageName,
                     OrderNumber = r.OrderNumber,
                     Status = StartListItemStatus.RequisitionToBeCreated,
                     ViewedByUser = GetViewedByUserName(r, userId),
                     OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                 }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests to be reported in method {nameof(BrokerActionListItems)}");
            }
            //Commented requisitions
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Requisition && r.RequisitionStatus == RequisitionStatus.Commented)
                .Select(r => new ActionStartListItemModel
                {
                    OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    EntityId = (int)r.RequestId,
                    LinkOverride = $"/Request/View/?tab=complaint",
                    InfoDate = r.AnsweredAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    LanguageName = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = StartListItemStatus.RequisitionCommented,
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Commented requisitions in method {nameof(BrokerActionListItems)}");
            }

            return actionList;
        }

        private List<StartListItemModel> BrokerWaitingListItems(int brokerId, int userId)
        {
            List<StartListItemModel> acceptedRequests = new List<StartListItemModel>();
            //Accepted requests (including requests that belong to group if AcceptedNewInterpreterAppointed)
            try
            {
                acceptedRequests = _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request && ((r.RequestGroupId == null & r.RequestStatus == RequestStatus.AnsweredAwaitingApproval) || r.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed) &&
                        r.StartAt > _clock.SwedenNow)
                    .Select(r => new StartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                        EntityId = (int)r.RequestId,
                        InfoDate = r.AnsweredAt.Value.DateTime,
                        InfoDateDescription = "Tillsatt: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.CustomerName,
                        LanguageName = r.LanguageName,
                        OrderNumber = r.OrderNumber,
                        Status = r.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed ? StartListItemStatus.NewInterpreterForApproval : StartListItemStatus.OrderAcceptedForApproval,
                        ViewedByUser = GetViewedByUserName(r, userId),
                        OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted requests in method {nameof(BrokerWaitingListItems)}");
            }
            try
            {
                acceptedRequests.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(rg => rg.RowType == StartListRowType.RequestGroup && rg.RequestGroupStatus == RequestStatus.AnsweredAwaitingApproval && !(rg.StartAt < _clock.SwedenNow))
                    .Select(rg => new StartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = rg.StartAt, EndDateTime = rg.EndAt },
                        EntityId = (int)rg.RequestGroupId,
                        InfoDate = rg.AnsweredAt.Value.DateTime,
                        InfoDateDescription = "Tillsatt: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)rg.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ExtraCompetenceLevel = (CompetenceAndSpecialistLevel?)rg.ExtraCompetencelevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        LinkOverride = $"/RequestGroup/View",
                        CustomerName = rg.CustomerName,
                        LanguageName = rg.LanguageName,
                        OrderNumber = rg.OrderGroupNumber,
                        Status = StartListItemStatus.OrderGroupAwaitingApproval,
                        ViewedByUser = GetViewedByUserName(rg, userId),
                        IsSingleOccasion = rg.IsSingleOccasion,
                        HasExtraInterpreter = rg.HasExtraInterpreter
                    }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted requestgroups is changed in method {nameof(BrokerWaitingListItems)}");
            }

            return acceptedRequests;
        }

        private List<StartListItemModel> BrokerApprovedListItems(int brokerId, int userId)
        {
            List<StartListItemModel> approvedRequests = new List<StartListItemModel>();

            //Approved requests (including individual requests that belong to a group) 
            List<StartListItemModel> approvedRequestAnswers = new List<StartListItemModel>();
            try
            {
                approvedRequestAnswers = _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.Approved && r.StartAt > _clock.SwedenNow)
                    .Select(r => new StartListItemModel
                    {
                        OrderDateTimeRange = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                        EntityId = (int)r.RequestId,
                        InfoDate = (r.RequestStatus == RequestStatus.Approved && r.AnswerProcessedAt.HasValue) ? r.AnswerProcessedAt.Value.DateTime : r.AnsweredAt.Value.DateTime,
                        InfoDateDescription = "Godkänd: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.CustomerName,
                        LanguageName = r.LanguageName,
                        OrderNumber = r.OrderNumber,
                        Status = StartListItemStatus.OrderApproved,
                        ViewedByUser = GetViewedByUserName(r, userId),
                        OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Approved requests in method {nameof(BrokerApprovedListItems)}");
            }
            return approvedRequestAnswers;
        }

        private static string GetViewedByUserName(BrokerStartListRow b, int userId)
        {
            return b.ViewedByUserId.HasValue && b.ViewedByUserId != userId ? $"{b.ViewedBy} håller på med detta ärende" : string.Empty;
        }

        private static DateTime? GetInfoDateForBroker(BrokerStartListRow r)
        {
            return (r.RequestStatus == RequestStatus.CancelledByCreator || r.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved) ? r.CancelledAt?.DateTime : r.RequestStatus == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.EntityDate.DateTime;
        }

        private static DateTime? GetInfoDateForGroupForBroker(BrokerStartListRow r)
        {
            return r.RequestGroupStatus == RequestStatus.ResponseNotAnsweredByCreator ? (r.LatestAnswerTimeForCustomer.HasValue ? r.LatestAnswerTimeForCustomer.Value.DateTime :
                        r.StartAt.DateTime) : (r.RequestGroupStatus != RequestStatus.DeniedByCreator && r.LastRequestCreatedUpdatedAt.HasValue) ? r.LastRequestCreatedUpdatedAt.Value.DateTime :
                        (r.RequestGroupStatus == RequestStatus.CancelledByCreator || r.RequestGroupStatus == RequestStatus.CancelledByCreatorWhenApproved) ? r.CancelledAt?.DateTime : r.RequestGroupStatus == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.EntityDate.DateTime;
        }

        private static StartListItemStatus GetStartListStatusForBroker(RequestStatus requestStatus, int replacingOrderId, bool isGroup = false)
        {
            if (requestStatus == RequestStatus.AcceptedAwaitingInterpreter && replacingOrderId == 0 && isGroup)
            {
                return StartListItemStatus.RequestGroupAccepted;
            }
            if (requestStatus == RequestStatus.AcceptedAwaitingInterpreter && replacingOrderId == 0 && !isGroup)
            {
                return StartListItemStatus.RequestAccepted;
            }
            if (requestStatus == RequestStatus.Received && replacingOrderId == 0 && isGroup)
            {
                return StartListItemStatus.RequestGroupReceived;
            }
            if (requestStatus == RequestStatus.Received && replacingOrderId == 0 && !isGroup)
            {
                return StartListItemStatus.RequestReceived;
            }
            if (requestStatus == RequestStatus.Received && replacingOrderId > 0)
            {
                return StartListItemStatus.ReplacementOrderRequestReceived;
            }
            if (requestStatus == RequestStatus.Created && replacingOrderId == 0 && isGroup)
            {
                return StartListItemStatus.RequestGroupArrived;
            }
            if (requestStatus == RequestStatus.Created && replacingOrderId == 0 && !isGroup)
            {
                return StartListItemStatus.RequestArrived;
            }
            if (requestStatus == RequestStatus.Created && replacingOrderId > 0)
            {
                return StartListItemStatus.ReplacementOrderRequestArrived;
            }
            if (requestStatus == RequestStatus.DeniedByCreator && isGroup)
            {
                return StartListItemStatus.RequestGroupDenied;
            }
            if (requestStatus == RequestStatus.DeniedByCreator && !isGroup)
            {
                return StartListItemStatus.RequestDenied;
            }
            if (requestStatus == RequestStatus.ResponseNotAnsweredByCreator && isGroup)
            {
                return StartListItemStatus.RespondedRequestGroupNotAnswered;
            }
            if (requestStatus == RequestStatus.ResponseNotAnsweredByCreator && !isGroup)
            {
                return StartListItemStatus.RespondedRequestNotAnswered;
            }
            return StartListItemStatus.OrderCancelled;
        }
        
        #endregion

        private static IEnumerable<StartList> GetInterpreterStartLists()
        {
            return Enumerable.Empty<StartList>();
        }

        private static IEnumerable<ConfirmationMessage> GetConfirmationMessages()
        {
            return Enumerable.Empty<ConfirmationMessage>();
        }

        public IActionResult About()
        {
            return View();
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public IActionResult UserManual()
        {
            return View();
        }

        public IActionResult Error()
        {
            _logger.LogError("TraceID: {0} UserID: {1}", Activity.Current?.Id ?? HttpContext.TraceIdentifier, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "-");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policies.TimeTravel)]
        public IActionResult TimeTravel(DateTime toDate, TimeSpan toTime, string action)
        {
            var clock = (TimeTravelClock)_clock;

            switch (action)
            {
                case "Jump":
                    var targetDateTime = toDate.Add(toTime).ToDateTimeOffsetSweden();
                    clock.TimeTravelTicks = targetDateTime.ToUniversalTime().Ticks - DateTimeOffset.UtcNow.Ticks;
                    break;
                case "Reset":
                    clock.TimeTravelTicks = 0;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Status(bool showDetails)
        {
            var status = await _verificationService.VerifySystemStatus();
            if (showDetails)
            {
                if ((await _authorizationService.AuthorizeAsync(User, status, Policies.View)).Succeeded)
                {
                    //Think I need to model bind, I do not want to send a BusinessLogic Model to a razor view...
                    return View(status.Items.Select(i => new StatusVerificationItemModel
                    {
                        Success = i.Success,
                        Test = i.Test
                    }));
                }
                else
                {
                    return Forbid();
                }
            }
            if (!status.Success)
            {
                return BadRequest();
            }
            return new EmptyResult();
        }
    }
}
