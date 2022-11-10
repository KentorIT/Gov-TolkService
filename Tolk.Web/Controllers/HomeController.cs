using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private IEnumerable<StartList> GetCustomerStartLists()
        {
            var actionList = new List<StartListItemModel>();
            var userId = User.GetUserId();
            var customerOrganisationId = User.GetCustomerOrganisationId();
            var customerUnits = User.TryGetAllCustomerUnits();
            try
            {
                //Accepted orders to approve, orders awaiting deadline
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits)
                    .Where(o => o.RowType == StartListRowType.Order && (o.OrderStatus == OrderStatus.RequestRespondedAwaitingApproval || o.OrderStatus == OrderStatus.AwaitingDeadlineFromCustomer)).ToList()
                    .Select(o => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = o.OrderId,
                        InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = o.OrderId,
                        Language = o.LanguageName,
                        OrderNumber = o.OrderNumber,
                        Status = GetStartListStatusForCustomer(o.OrderStatus, o.ReplacingOrderId ?? 0),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && o.LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)o.LatestAnswerTimeForCustomer.Value.DateTime : null,
                        ButtonAction = "View",
                        ButtonController = "Order"
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted orders to approve, orders awaiting deadline in method {nameof(GetCustomerStartLists)}");
            }
            //Orders to approve where interpreter is changed
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.RequestRespondedNewInterpreter).ToList()
                    .Select(o => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = o.OrderId,
                        InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = o.OrderId,
                        Language = o.LanguageName,
                        OrderNumber = o.OrderNumber,
                        Status = GetStartListStatusForCustomer(o.OrderStatus, o.ReplacingOrderId ?? 0),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && o.LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)o.LatestAnswerTimeForCustomer.Value.DateTime : null,
                        ButtonAction = "View",
                        ButtonController = "Order",
                        OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders to approve where interpreter is changed in method {nameof(GetCustomerStartLists)}");
            }
            //Accepted ordergroups to approve 
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)))
                {
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.RequestRespondedAwaitingApproval).ToList()
                    .Select(og => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "OrderGroup",
                        DefaultItemId = (int)og.OrderGroupId,
                        InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)og.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ExtraCompetenceLevel = (CompetenceAndSpecialistLevel?)og.ExtraCompetencelevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = (int)og.OrderGroupId,
                        Language = og.LanguageName,
                        OrderNumber = og.OrderGroupNumber,
                        Status = GetStartListStatusForCustomer((OrderStatus)og.OrderGroupStatus, 0, true),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && og.LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)og.LatestAnswerTimeForCustomer.Value.DateTime : null,
                        ButtonAction = "View",
                        ButtonController = "OrderGroup",
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                    //Order groups awaiting deadline from customer (customer should set last answer date)
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.AwaitingDeadlineFromCustomer).ToList()
                    .Select(og => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "OrderGroup",
                        DefaultItemId = (int)og.OrderGroupId,
                        InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = (int)og.OrderGroupId,
                        Language = og.LanguageName,
                        OrderNumber = og.OrderGroupNumber,
                        Status = GetStartListStatusForCustomer((OrderStatus)og.OrderGroupStatus, 0, true),
                        ButtonAction = "View",
                        ButtonController = "OrderGroup",
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted ordergroups to approve in method {nameof(GetCustomerStartLists)}");
            }
            try
            {
                if (_options.AllowDeclineExtraInterpreterOnRequestGroups)
                {
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.RequestAwaitingPartialAccept).ToList()
                        .Select(og => new StartListItemModel
                        {
                            Orderdate = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                            DefaulListAction = "View",
                            DefaulListController = "OrderGroup",
                            DefaultItemId = (int)og.OrderGroupId,
                            InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                            CompetenceLevel = (CompetenceAndSpecialistLevel?)og.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,//this might not be correct
                            ButtonItemId = (int)og.OrderGroupId,
                            Language = og.LanguageName,
                            OrderNumber = og.OrderGroupNumber,
                            Status = GetStartListStatusForCustomer((OrderStatus)og.OrderGroupStatus, 0, true),
                            ButtonAction = "View",
                            ButtonController = "OrderGroup",
                            IsSingleOccasion = og.IsSingleOccasion,
                            HasExtraInterpreter = og.HasExtraInterpreter,
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for RequestAwaitingPartialAccept in method {nameof(GetCustomerStartLists)}");
            }
            //Orders not answered by creator, no broker accepted order (must include groups since change of interpreter can handle LatestAnswerTimeForCustomer)
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Where(o => o.RowType == StartListRowType.Order && (((!o.OrderGroupId.HasValue || o.OrderGroupStatus != OrderStatus.NoBrokerAcceptedOrder) && o.OrderStatus == OrderStatus.NoBrokerAcceptedOrder) ||
                            (_options.EnableSetLatestAnswerTimeForCustomer && (!o.OrderGroupId.HasValue || o.OrderGroupStatus != OrderStatus.ResponseNotAnsweredByCreator) && o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator))).ToList()
                        .Select(o => new StartListItemModel
                        {
                            Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                            DefaulListAction = "View",
                            DefaulListController = "Order",
                            DefaultItemId = o.OrderId,
                            InfoDate = (o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator) ? o.LatestAnswerTimeForCustomer?.DateTime ?? o.StartAt.DateTime : GetInfoDateForCustomer(o)?.DateTime,
                            CompetenceLevel = o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator ? (CompetenceAndSpecialistLevel?)o.CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter,
                            ButtonItemId = o.OrderId,
                            Language = o.LanguageName,
                            OrderNumber = o.OrderNumber,
                            Status = o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator ? StartListItemStatus.RespondedRequestNotAnswered : o.ReplacingOrderId != null ? StartListItemStatus.ReplacementOrderNotAnswered : StartListItemStatus.OrderNotAnswered,
                            ButtonAction = "View",
                            ButtonController = "Order",
                            OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                        }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders not answered by creator, no broker accepted order in method {nameof(GetCustomerStartLists)}");
            }
            //Ordergroups not answered by creator, no broker accepted order
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)))
                {
                    actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Where(og => og.RowType == StartListRowType.OrderGroup && ((og.OrderGroupStatus == OrderStatus.NoBrokerAcceptedOrder)
                            || (_options.EnableSetLatestAnswerTimeForCustomer && og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator))).ToList()
                        .Select(og => new StartListItemModel
                        {
                            Orderdate = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                            DefaulListAction = "View",
                            DefaulListController = "OrderGroup",
                            DefaultItemId = (int)og.OrderGroupId,
                            InfoDate = og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator ? og.LatestAnswerTimeForCustomer?.DateTime ?? og.StartAt.DateTime : GetInfoDateForCustomer(og)?.DateTime,
                            CompetenceLevel = og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator ? (CompetenceAndSpecialistLevel?)og.CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter,
                            ButtonItemId = (int)og.OrderGroupId,
                            Language = og.LanguageName,
                            OrderNumber = og.OrderGroupNumber,
                            Status = og.OrderGroupStatus == OrderStatus.ResponseNotAnsweredByCreator ? StartListItemStatus.RespondedRequestGroupNotAnswered : StartListItemStatus.OrderGroupNotAnswered,
                            ButtonAction = "View",
                            ButtonController = "OrderGroup",
                            IsSingleOccasion = og.IsSingleOccasion,
                            HasExtraInterpreter = og.HasExtraInterpreter,
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Ordergroups not answered by creator, no broker accepted order in method {nameof(GetCustomerStartLists)}");
            }
            //Cancelled by broker
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.CancelledByBroker).ToList()
                .Select(o => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Order",
                    DefaultItemId = o.OrderId,
                    InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel,
                    ButtonItemId = o.OrderId,
                    Language = o.LanguageName,
                    OrderNumber = o.OrderNumber,
                    Status = StartListItemStatus.OrderCancelled,
                    ButtonAction = "View",
                    ButtonController = "Order",
                    OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders cancelled by broker in method {nameof(GetCustomerStartLists)}");
            }
            //Requisitions to process
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeContact: true, includeOrderGroupOrders: true)
                    .Where(r => r.RowType == StartListRowType.Requisition)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = r.OrderId,
                        DefaultItemTab = "requisition",
                        InfoDate = r.EntityDate.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = r.OrderId,
                        Language = r.LanguageName,
                        OrderNumber = r.OrderNumber,
                        Status = StartListItemStatus.RequisitonArrived,
                        ButtonAction = "View",
                        ButtonController = "Order",
                        ButtonItemTab = "requisition",
                        OrderGroupNumber = r.OrderGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requisitions to review in method {nameof(GetCustomerStartLists)}");
            }
            //Disputed complaints
            try
            {
                actionList.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeContact: true, includeOrderGroupOrders: true)
                    .Where(c => c.RowType == StartListRowType.Complaint)
                    .Select(c => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = c.StartAt, EndDateTime = c.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = c.OrderId,
                        DefaultItemTab = "complaint",
                        InfoDate = c.AnsweredAt.HasValue ? c.AnsweredAt.Value.DateTime : c.EntityDate.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)c.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = c.OrderId,
                        Language = c.LanguageName,
                        OrderNumber = c.OrderNumber,
                        Status = StartListItemStatus.ComplaintEvent,
                        ButtonAction = "View",
                        ButtonController = "Order",
                        ButtonItemTab = "complaint",
                        OrderGroupNumber = c.OrderGroupId.HasValue ? $"Del av {c.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Disputed complaints in method {nameof(GetCustomerStartLists)}");
            }

            var count = actionList.Any() ? actionList.Count : 0;

            yield return new StartList
            {
                Header = count > 0 ? $"Kräver handling av myndighet ({count} st)" : "Kräver handling av myndighet",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som kräver handling av myndigheten",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            List<StartListItemModel> sentOrders = new List<StartListItemModel>();
            //Sent orders
            try
            {
                sentOrders = _dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, false, false)
                    .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.Requested
                    && o.EndAt > _clock.SwedenNow)
                    .Select(o => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = o.OrderId,
                        InfoDate = o.EntityDate.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        Language = o.LanguageName,
                        OrderNumber = o.OrderNumber,
                        Status = o.ReplacingOrderId.HasValue ? StartListItemStatus.ReplacementOrderCreated : StartListItemStatus.OrderCreated
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Sent orders in method {nameof(GetCustomerStartLists)}");
            }
            //Sent ordergroups
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderGroups)))
                {
                    sentOrders.AddRange(_dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Where(og => og.RowType == StartListRowType.OrderGroup && og.OrderGroupStatus == OrderStatus.Requested && og.EndAt > _clock.SwedenNow).ToList()
                    .Select(og => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = og.StartAt, EndDateTime = og.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "OrderGroup",
                        DefaultItemId = (int)og.OrderGroupId,
                        InfoDate = og.EntityDate.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        Language = og.LanguageName,
                        OrderNumber = og.OrderGroupNumber,
                        Status = StartListItemStatus.OrderGroupCreated,
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Sent ordergroups in method {nameof(GetCustomerStartLists)}");
            }

            count = sentOrders.Any() ? sentOrders.Count : 0;
            yield return new StartList
            {
                Header = count > 0 ? $"Skickade bokningar ({count} st)" : "Skickade bokningsförfrågningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är skickade",
                StartListObjects = sentOrders
            };

            //Approved orders
            List<StartListItemModel> approvedOrders = new List<StartListItemModel>();
            try
            {
                approvedOrders = _dbContext.CustomerStartListRows.CustomerStartListRows(customerOrganisationId, userId, customerUnits, false, true)
                    .Where(o => o.RowType == StartListRowType.Order && o.OrderStatus == OrderStatus.ResponseAccepted && o.EndAt > _clock.SwedenNow)
                .Select(o => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Order",
                    DefaultItemId = o.OrderId,
                    InfoDate = o.AnsweredAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel)o.CompetenceLevel,
                    Language = o.LanguageName,
                    OrderNumber = o.OrderNumber,
                    Status = StartListItemStatus.OrderApproved,
                    OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.OrderGroupNumber}" : string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Approved orders in method {nameof(GetCustomerStartLists)}");
            }
            count = approvedOrders.Any() ? approvedOrders.Count : 0;

            yield return new StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = approvedOrders
            };
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

        private IEnumerable<StartList> GetBrokerStartLists()
        {
            var brokerId = User.GetBrokerId();
            var userId = User.GetUserId();
            var actionList = new List<StartListItemModel>();
            //Requests with status received, created
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RequestGroupId == null && r.RowType == StartListRowType.Request && (r.RequestStatus == RequestStatus.Created || r.RequestStatus == RequestStatus.Received))
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    DefaulListAction = "Process",
                    DefaulListController = "Request",
                    DefaultItemId = (int)r.RequestId,
                    InfoDate = r.LastRequestCreatedUpdatedAt.HasValue ? (DateTime?)r.LastRequestCreatedUpdatedAt.Value.DateTime : GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    ButtonItemId = (int)r.RequestId,
                    Language = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    ButtonAction = "Process",
                    ButtonController = "Request",
                    LatestDate = r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null,
                    ViewedByUser = GetViewedByUserName(r, userId)
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests with status received, created in method {nameof(GetBrokerStartLists)}");
            }
            //Denied requests 
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroupStatus != RequestStatus.DeniedByCreator)) &&
                    r.RequestStatus == RequestStatus.DeniedByCreator)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = (int)r.RequestId,
                    InfoDate = GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    ButtonItemId = (int)r.RequestId,
                    Language = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Denied requests in method {nameof(GetBrokerStartLists)}");
            }
            //Requests with status CancelledByCreatorWhenApproved
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    DefaulListAction = r.RequestIsToBeProcessedByBroker ? "Process" : "View",
                    DefaulListController = "Request",
                    DefaultItemId = (int)r.RequestId,
                    InfoDate = GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    ButtonItemId = (int)r.RequestId,
                    Language = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    ButtonAction = r.RequestIsToBeProcessedByBroker ? "Process" : "View",
                    ButtonController = "Request",
                    LatestDate = (r.RequestStatus == RequestStatus.Created || r.RequestStatus == RequestStatus.Received) ? (r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null) : null,
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests with status CancelledByCreatorWhenApproved in method {nameof(GetBrokerStartLists)}");
            }
            //Non answered responded requests 
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroupStatus != RequestStatus.ResponseNotAnsweredByCreator)) && r.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    DefaulListAction = r.RequestIsToBeProcessedByBroker ? "Process" : "View",
                    DefaulListController = "Request",
                    DefaultItemId = (int)r.RequestId,
                    InfoDate = r.LatestAnswerTimeForCustomer.HasValue ? r.LatestAnswerTimeForCustomer.Value.DateTime : r.StartAt.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    ButtonItemId = (int)r.RequestId,
                    Language = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)r.RequestStatus, r.ReplacingOrderId ?? 0, false),
                    ButtonAction = r.RequestIsToBeProcessedByBroker ? "Process" : "View",
                    ButtonController = "Request",
                    LatestDate = r.RequestIsToBeProcessedByBroker ? (r.RequestExpiresAt.HasValue ? (DateTime?)r.RequestExpiresAt.Value.DateTime : null) : null,
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Non answered responded requests in method {nameof(GetBrokerStartLists)}");
            }
            //Non confirmed order changes
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && r.EndAt > _clock.SwedenNow && (r.RequestStatus == RequestStatus.Approved || r.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed) && r.OrderChangedAt.HasValue)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = (int)r.RequestId,
                    InfoDate = r.OrderChangedAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    ButtonItemId = (int)r.RequestId,
                    Language = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = StartListItemStatus.OrderChanged,
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Non confirmed order changes in method {nameof(GetBrokerStartLists)}");
            }
            //Requestgroups status received, created, denied, not answered by customer
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(rg => rg.RowType == StartListRowType.RequestGroup && (rg.RequestGroupStatus == RequestStatus.Created || rg.RequestGroupStatus == RequestStatus.Received || rg.RequestGroupStatus == RequestStatus.DeniedByCreator || rg.RequestGroupStatus == RequestStatus.ResponseNotAnsweredByCreator))
                .Select(rg => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = rg.StartAt, EndDateTime = rg.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "RequestGroup",
                    DefaultItemId = (int)rg.RequestGroupId,
                    InfoDate = GetInfoDateForGroupForBroker(rg),
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)rg.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    ExtraCompetenceLevel = (CompetenceAndSpecialistLevel?)rg.ExtraCompetencelevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = rg.CustomerName,
                    ButtonItemId = (int)rg.RequestGroupId,
                    Language = rg.LanguageName,
                    OrderNumber = rg.OrderGroupNumber,
                    Status = GetStartListStatusForBroker((RequestStatus)rg.RequestGroupStatus, 0, true),
                    ButtonAction = "View",
                    ButtonController = "RequestGroup",
                    LatestDate = rg.RequestGroupIsToBeProcessedByBroker ? (rg.RequestExpiresAt.HasValue ? (DateTime?)rg.RequestExpiresAt.Value.DateTime : null) : null,
                    ViewedByUser = GetViewedByUserName(rg, userId),
                    IsSingleOccasion = rg.IsSingleOccasion,
                    HasExtraInterpreter = rg.HasExtraInterpreter
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requestgroups status received, created, denied, not answered by customer in method {nameof(GetBrokerStartLists)}");
            }
            //Complaints
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(c => c.ComplaintStatus == ComplaintStatus.Created)
                .Select(c => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = c.StartAt, EndDateTime = c.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = (int)c.RequestId,
                    DefaultItemTab = "complaint",
                    InfoDate = c.EntityDate.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)c.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = c.CustomerName,
                    ButtonItemId = (int)c.RequestId,
                    Language = c.LanguageName,
                    OrderNumber = c.OrderNumber,
                    Status = StartListItemStatus.ComplaintEvent,
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ButtonItemTab = "complaint",
                    ViewedByUser = GetViewedByUserName(c, userId),
                    OrderGroupNumber = c.RequestGroupId.HasValue ? $"Del av {c.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Complaints in method {nameof(GetBrokerStartLists)}");
            }
            //Requests to be reported
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.Approved && r.StartAt < _clock.SwedenNow)
                 .Select(r => new StartListItemModel
                 {
                     Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                     DefaulListAction = "View",
                     DefaulListController = "Request",
                     DefaultItemId = (int)r.RequestId,
                     InfoDate = r.EndAt.DateTime,
                     InfoDateDescription = "Utfört: ",
                     CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                     CustomerName = r.CustomerName,
                     ButtonItemId = (int)r.RequestId,
                     Language = r.LanguageName,
                     OrderNumber = r.OrderNumber,
                     Status = StartListItemStatus.RequisitionToBeCreated,
                     ButtonAction = "View",
                     ButtonController = "Request",
                     ViewedByUser = GetViewedByUserName(r, userId),
                     OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                 }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests to be reported in method {nameof(GetBrokerStartLists)}");
            }
            //Commented requisitions
            try
            {
                actionList.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(r => r.RowType == StartListRowType.Requisition && r.RequisitionStatus == RequisitionStatus.Commented)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = (int)r.RequestId,
                    DefaultItemTab = "requisition",
                    InfoDate = r.AnsweredAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.CustomerName,
                    ButtonItemId = (int)r.RequestId,
                    Language = r.LanguageName,
                    OrderNumber = r.OrderNumber,
                    Status = StartListItemStatus.RequisitionCommented,
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ButtonItemTab = "requisition",
                    ViewedByUser = GetViewedByUserName(r, userId),
                    OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Commented requisitions in method {nameof(GetBrokerStartLists)}");
            }
            var count = actionList.Any() ? actionList.Count : 0;
            yield return new StartList
            {
                Header = count > 0 ? $"Kräver handling av förmedling ({count} st)" : "Kräver handling av förmedling",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som kräver handling av förmedling",
                StartListObjects = actionList,
                HasReviewAction = true,
                DisplayCustomer = true
            };
            //Accepted requests (including requests that belong to group if AcceptedNewInterpreterAppointed)
            List<StartListItemModel> acceptedRequests = new List<StartListItemModel>();
            try
            {
                acceptedRequests = _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request && ((r.RequestGroupId == null & r.RequestStatus == RequestStatus.AcceptedAwaitingApproval) || r.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed) &&
                        r.StartAt > _clock.SwedenNow)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Request",
                        DefaultItemId = (int)r.RequestId,
                        InfoDate = r.AnsweredAt.Value.DateTime,
                        InfoDateDescription = "Tillsatt: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.CustomerName,
                        Language = r.LanguageName,
                        OrderNumber = r.OrderNumber,
                        Status = r.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed ? StartListItemStatus.NewInterpreterForApproval : StartListItemStatus.OrderAcceptedForApproval,
                        ViewedByUser = GetViewedByUserName(r, userId),
                        OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted requests in method {nameof(GetBrokerStartLists)}");
            }
            try
            {
                acceptedRequests.AddRange(_dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                .Where(rg => rg.RowType == StartListRowType.RequestGroup && rg.RequestGroupStatus == RequestStatus.AcceptedAwaitingApproval && !(rg.StartAt < _clock.SwedenNow))
                .Select(rg => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = rg.StartAt, EndDateTime = rg.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "RequestGroup",
                    DefaultItemId = (int)rg.RequestGroupId,
                    InfoDate = rg.AnsweredAt.Value.DateTime,
                    InfoDateDescription = "Tillsatt: ",
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)rg.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    ExtraCompetenceLevel = (CompetenceAndSpecialistLevel?)rg.ExtraCompetencelevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = rg.CustomerName,
                    Language = rg.LanguageName,
                    OrderNumber = rg.OrderGroupNumber,
                    Status = StartListItemStatus.OrderGroupAwaitingApproval,
                    ViewedByUser = GetViewedByUserName(rg, userId),
                    IsSingleOccasion = rg.IsSingleOccasion,
                    HasExtraInterpreter = rg.HasExtraInterpreter
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted requestgroups is changed in method {nameof(GetBrokerStartLists)}");
            }
            count = acceptedRequests.Any() ? acceptedRequests.Count : 0;

            yield return new StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar som inväntar godkännande ({count} st)" : "Tillsatta bokningar som inväntar godkännande",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga tillsatta bokningar som inväntar godkännande",
                StartListObjects = acceptedRequests,
                DisplayCustomer = true
            };

            //Approved requests (including individual requests that belong to a group) 
            List<StartListItemModel> approvedRequestAnswers = new List<StartListItemModel>();
            try
            {
                approvedRequestAnswers = _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.Approved && r.StartAt > _clock.SwedenNow)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.StartAt, EndDateTime = r.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Request",
                        DefaultItemId = (int)r.RequestId,
                        InfoDate = (r.RequestStatus == RequestStatus.Approved && r.AnswerProcessedAt.HasValue) ? r.AnswerProcessedAt.Value.DateTime : r.AnsweredAt.Value.DateTime,
                        InfoDateDescription = "Godkänd: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.CustomerName,
                        Language = r.LanguageName,
                        OrderNumber = r.OrderNumber,
                        Status = StartListItemStatus.OrderApproved,
                        ViewedByUser = GetViewedByUserName(r, userId),
                        OrderGroupNumber = r.RequestGroupId.HasValue ? $"Del av {r.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Approved requests in method {nameof(GetBrokerStartLists)}");
            }
            count = approvedRequestAnswers.Any() ? approvedRequestAnswers.Count : 0;

            yield return new StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = approvedRequestAnswers,
                DisplayCustomer = true
            };
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
            return (requestStatus == RequestStatus.Received && replacingOrderId == 0) ?
                isGroup ? StartListItemStatus.RequestGroupReceived : StartListItemStatus.RequestReceived
                : (requestStatus == RequestStatus.Received && replacingOrderId > 0) ? StartListItemStatus.ReplacementOrderRequestReceived
                : (requestStatus == RequestStatus.Created && replacingOrderId == 0) ?
                isGroup ? StartListItemStatus.RequestGroupArrived : StartListItemStatus.RequestArrived
                : (requestStatus == RequestStatus.Created && replacingOrderId > 0) ? StartListItemStatus.ReplacementOrderRequestArrived
                : requestStatus == RequestStatus.DeniedByCreator ? isGroup ? StartListItemStatus.RequestGroupDenied : StartListItemStatus.RequestDenied
                : requestStatus == RequestStatus.ResponseNotAnsweredByCreator ? isGroup ? StartListItemStatus.RespondedRequestGroupNotAnswered : StartListItemStatus.RespondedRequestNotAnswered
                : StartListItemStatus.OrderCancelled;
        }

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
