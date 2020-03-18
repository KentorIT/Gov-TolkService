using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catch the general exceptions and log, the startpage must be displayed")]

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
            return View(new StartViewModel
            {
                PageTitle = (User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator)) ? $"Startsida för {Constants.SystemName}" : "Aktiva bokningar",
                Message = message,
                ErrorMessage = errorMessage,
                ConfirmationMessages = GetConfirmationMessages(),
                SystemMessages = SystemMessagesForUser,
                StartLists = await GetStartLists(),
                IsBroker = User.TryGetBrokerId().HasValue,
                IsCustomer = User.TryGetCustomerOrganisationId().HasValue
            });
        }

        private IEnumerable<SystemMessage> SystemMessagesForUser
        {
            get
            {
                bool displayBrokerMessages = !User.TryGetBrokerId().HasValue ? User.IsInRole(Roles.SystemAdministrator) ? true : false : true;
                bool displayCustomerMessages = !User.TryGetCustomerOrganisationId().HasValue ? User.IsInRole(Roles.SystemAdministrator) ? true : false : true;
                bool displayCentralAdministratorMessages = !User.IsInRole(Roles.CentralAdministrator) ? User.IsInRole(Roles.SystemAdministrator) ? true : false : true;

                return _dbContext.SystemMessages
                    .Where(s => s.ActiveFrom < _clock.SwedenNow
                    && s.ActiveTo.Date >= _clock.SwedenNow.Date
                    && (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.All
                    || (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.BrokerUsers && displayBrokerMessages)
                    || (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.CustomerUsers && displayCustomerMessages)
                    || (s.SystemMessageUserTypeGroup == SystemMessageUserTypeGroup.CentralAdministrators && displayCentralAdministratorMessages)))
                    .ToList().OrderByDescending(s => s.SystemMessageType)
                .ThenByDescending(s => s.LastUpdatedCreatedAt);
            }
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
                actionList.AddRange(_dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits)
                    .Include(o => o.Language)
                    .Include(o => o.Requests)
                    .Where(o => o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.AwaitingDeadlineFromCustomer).ToList()
                    .Select(o => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = o.OrderId,
                        InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).First().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = o.OrderId,
                        Language = o.OtherLanguage ?? o.Language.Name,
                        OrderNumber = o.OrderNumber,
                        Status = GetStartListStatusForCustomer(o.Status, o.ReplacingOrderId ?? 0),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && o.Requests.OrderByDescending(r => r.RequestId).First().LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)o.Requests.OrderByDescending(r => r.RequestId).First().LatestAnswerTimeForCustomer.Value.DateTime : null,
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
                actionList.AddRange(_dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                    .Include(o => o.Language)
                    .Include(o => o.Requests)
                    .Include(o => o.Group)
                    .Where(o => o.Status == OrderStatus.RequestRespondedNewInterpreter).ToList()
                    .Select(o => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = o.OrderId,
                        InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).First().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = o.OrderId,
                        Language = o.OtherLanguage ?? o.Language.Name,
                        OrderNumber = o.OrderNumber,
                        Status = GetStartListStatusForCustomer(o.Status, o.ReplacingOrderId ?? 0),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && o.Requests.OrderByDescending(r => r.RequestId).First().LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)o.Requests.OrderByDescending(r => r.RequestId).First().LatestAnswerTimeForCustomer.Value.DateTime : null,
                        ButtonAction = "View",
                        ButtonController = "Order",
                        OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.Group.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders to approve where interpreter is changed in method {nameof(GetCustomerStartLists)}");
            }
            //Accepted ordergroups to approve 
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UseOrderGroups))
                {
                    actionList.AddRange(_dbContext.OrderGroups.CustomerOrderGroups(customerOrganisationId, userId, customerUnits)
                    .Include(og => og.Language)
                    .Include(og => og.Orders)
                    .Include(og => og.RequestGroups).ThenInclude(rg => rg.Requests)
                    .Where(og => og.Status == OrderStatus.RequestResponded).ToList()
                    .Select(og => new StartListItemModel
                    {
                        Orderdate = og.Orders.OrderBy(v => v.StartAt).Select(o => new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }).FirstOrDefault(),
                        DefaulListAction = "View",
                        DefaulListController = "OrderGroup",
                        DefaultItemId = og.OrderGroupId,
                        InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)og.RequestGroups.Where(req => req.Status == RequestStatus.Accepted).First()
                            .Requests.Where(req => req.Status == RequestStatus.Accepted).First().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ExtraCompetenceLevel = !og.HasExtraInterpreter ? CompetenceAndSpecialistLevel.NoInterpreter :
                            (CompetenceAndSpecialistLevel?)og.RequestGroups.First(req => req.Status == RequestStatus.Accepted).FirstRequestForExtraInterpreter.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = og.OrderGroupId,
                        Language = og.OtherLanguage ?? og.Language.Name,
                        OrderNumber = og.OrderGroupNumber,
                        Status = GetStartListStatusForCustomer(og.Status, 0, true),
                        LatestDate = (_options.EnableSetLatestAnswerTimeForCustomer && og.RequestGroups.Where(req => req.Status == RequestStatus.Accepted).First().LatestAnswerTimeForCustomer.HasValue) ? (DateTime?)og.RequestGroups.Where(req => req.Status == RequestStatus.Accepted).First().LatestAnswerTimeForCustomer.Value.DateTime : null,
                        ButtonAction = "View",
                        ButtonController = "OrderGroup",
                        IsSingleOccasion = og.IsSingleOccasion,
                        HasExtraInterpreter = og.HasExtraInterpreter,
                    }));
                    //Order groups awaiting deadline from customer (customer should set last answer date)
                    actionList.AddRange(_dbContext.OrderGroups.CustomerOrderGroups(customerOrganisationId, userId, customerUnits)
                    .Include(og => og.Language)
                    .Include(og => og.Orders)
                    .Include(og => og.RequestGroups)
                    .Where(og => og.Status == OrderStatus.AwaitingDeadlineFromCustomer).ToList()
                    .Select(og => new StartListItemModel
                    {
                        Orderdate = og.Orders.OrderBy(v => v.StartAt).Select(o => new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }).FirstOrDefault(),
                        DefaulListAction = "View",
                        DefaulListController = "OrderGroup",
                        DefaultItemId = og.OrderGroupId,
                        InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = og.OrderGroupId,
                        Language = og.OtherLanguage ?? og.Language.Name,
                        OrderNumber = og.OrderGroupNumber,
                        Status = GetStartListStatusForCustomer(og.Status, 0, true),
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
                    actionList.AddRange(_dbContext.OrderGroups.CustomerOrderGroups(customerOrganisationId, userId, customerUnits)
                        .Include(og => og.Language)
                        .Include(og => og.Orders)
                        .Include(og => og.RequestGroups).ThenInclude(rg => rg.Requests)
                        .Where(og => og.Status == OrderStatus.RequestAwaitingPartialAccept).ToList()
                        .Select(og => new StartListItemModel
                        {
                            Orderdate = og.Orders.OrderBy(v => v.StartAt).Select(o => new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }).FirstOrDefault(),
                            DefaulListAction = "View",
                            DefaulListController = "OrderGroup",
                            DefaultItemId = og.OrderGroupId,
                            InfoDate = GetInfoDateForCustomer(og)?.DateTime,
                            CompetenceLevel = (CompetenceAndSpecialistLevel?)og.RequestGroups.Where(req => req.Status == RequestStatus.PartiallyAccepted).First()
                                .Requests.Where(req => req.Status == RequestStatus.Accepted).First().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                            ButtonItemId = og.OrderGroupId,
                            Language = og.OtherLanguage ?? og.Language.Name,
                            OrderNumber = og.OrderGroupNumber,
                            Status = GetStartListStatusForCustomer(og.Status, 0, true),
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
                actionList.AddRange(_dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                        .Include(o => o.Language)
                        .Include(o => o.Requests)
                        .Include(o => o.Group)
                        .Include(o => o.OrderStatusConfirmations)
                        .Where(o => (!o.OrderGroupId.HasValue && o.Status == OrderStatus.NoBrokerAcceptedOrder && !o.OrderStatusConfirmations.Any(s => s.OrderStatus == OrderStatus.NoBrokerAcceptedOrder)) ||
                            (_options.EnableSetLatestAnswerTimeForCustomer &&
                            (!o.OrderGroupId.HasValue || o.Group.Status != OrderStatus.ResponseNotAnsweredByCreator) &&
                             o.Status == OrderStatus.ResponseNotAnsweredByCreator &&
                            !o.OrderStatusConfirmations.Any(s => s.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator))).ToList()
                        .Select(o => new StartListItemModel
                        {
                            Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                            DefaulListAction = "View",
                            DefaulListController = "Order",
                            DefaultItemId = o.OrderId,
                            InfoDate = (o.Status == OrderStatus.ResponseNotAnsweredByCreator) ? o.Requests.OrderByDescending(r => r.RequestId).First().LatestAnswerTimeForCustomer?.DateTime ?? o.StartAt.DateTime : GetInfoDateForCustomer(o)?.DateTime,
                            CompetenceLevel = o.Status == OrderStatus.ResponseNotAnsweredByCreator ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).First().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter,
                            ButtonItemId = o.OrderId,
                            Language = o.OtherLanguage ?? o.Language.Name,
                            OrderNumber = o.OrderNumber,
                            Status = o.Status == OrderStatus.ResponseNotAnsweredByCreator ? StartListItemStatus.RespondedRequestNotAnswered : o.ReplacingOrderId != null ? StartListItemStatus.ReplacementOrderNotAnswered : StartListItemStatus.OrderNotAnswered,
                            ButtonAction = "View",
                            ButtonController = "Order",
                            OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.Group.OrderGroupNumber}" : string.Empty
                        }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders not answered by creator, no broker accepted order in method {nameof(GetCustomerStartLists)}");
            }
            //Ordergroups not answered by creator, no broker accepted order
            try
            {
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UseOrderGroups))
                {
                    actionList.AddRange(_dbContext.OrderGroups.CustomerOrderGroups(customerOrganisationId, userId, customerUnits)
                        .Include(og => og.Language)
                        .Include(og => og.RequestGroups).ThenInclude(rg => rg.Requests)
                        .Include(og => og.StatusConfirmations)
                        .Include(og => og.Orders)
                        .Where(og => (og.Status == OrderStatus.NoBrokerAcceptedOrder && !og.StatusConfirmations.Any(s => s.OrderStatus == OrderStatus.NoBrokerAcceptedOrder))
                            || (_options.EnableSetLatestAnswerTimeForCustomer && og.Status == OrderStatus.ResponseNotAnsweredByCreator && !og.StatusConfirmations.Any(s => s.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator))).ToList()
                        .Select(og => new StartListItemModel
                        {
                            Orderdate = og.Orders.OrderBy(v => v.StartAt).Select(o => new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }).FirstOrDefault(),
                            DefaulListAction = "View",
                            DefaulListController = "OrderGroup",
                            DefaultItemId = og.OrderGroupId,
                            InfoDate = og.Status == OrderStatus.ResponseNotAnsweredByCreator ? og.RequestGroups.OrderBy(req => req.RequestGroupId).Last().LatestAnswerTimeForCustomer?.DateTime ?? og.Orders.OrderBy(v => v.StartAt).First().StartAt.DateTime : GetInfoDateForCustomer(og)?.DateTime,
                            CompetenceLevel = og.Status == OrderStatus.ResponseNotAnsweredByCreator ? (CompetenceAndSpecialistLevel?)og.RequestGroups.OrderBy(req => req.RequestGroupId).Last()
                                .Requests.OrderBy(req => req.RequestId).Last().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter,
                            ButtonItemId = og.OrderGroupId,
                            Language = og.OtherLanguage ?? og.Language.Name,
                            OrderNumber = og.OrderGroupNumber,
                            Status = og.Status == OrderStatus.ResponseNotAnsweredByCreator ? StartListItemStatus.RespondedRequestGroupNotAnswered : StartListItemStatus.OrderGroupNotAnswered,
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
                actionList.AddRange(_dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, includeOrderGroupOrders: true)
                .Include(o => o.Language)
                .Include(o => o.Requests).ThenInclude(r => r.RequestStatusConfirmations)
                .Include(o => o.Group)
                .Where(o => o.Status == OrderStatus.CancelledByBroker && o.Requests.Any(r => r.Status == RequestStatus.CancelledByBroker &&
                    !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker))).ToList()
                .Select(o => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Order",
                    DefaultItemId = o.OrderId,
                    InfoDate = GetInfoDateForCustomer(o)?.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)o.Requests.Single(r => r.Status == RequestStatus.CancelledByBroker).CompetenceLevel,
                    ButtonItemId = o.OrderId,
                    Language = o.OtherLanguage ?? o.Language.Name,
                    OrderNumber = o.OrderNumber,
                    Status = StartListItemStatus.OrderCancelled,
                    ButtonAction = "View",
                    ButtonController = "Order",
                    OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.Group.OrderGroupNumber}" : string.Empty
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Orders cancelled by broker in method {nameof(GetCustomerStartLists)}");
            }
            //Requisitions to review
            try
            {
                actionList.AddRange(_dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, includeContact: true, includeOrderGroupOrders: true)
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .SelectMany(o => o.Requests)
                    .SelectMany(r => r.Requisitions)
                    .Where(r => r.Status == RequisitionStatus.Created)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = r.Request.Order.OrderId,
                        DefaultItemTab = "requisition",
                        InfoDate = r.CreatedAt.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = r.Request.OrderId,
                        Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name,
                        OrderNumber = r.Request.Order.OrderNumber,
                        Status = StartListItemStatus.RequisitonArrived,
                        ButtonAction = "View",
                        ButtonController = "Order",
                        ButtonItemTab = "requisition",
                        OrderGroupNumber = r.Request.Order.OrderGroupId.HasValue ? $"Del av {r.Request.Order.Group.OrderGroupNumber}" : string.Empty
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requisitions to review in method {nameof(GetCustomerStartLists)}");
            }
            //Disputed complaints
            try
            {
                actionList.AddRange(_dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, includeContact: true, includeOrderGroupOrders: true)
                    .SelectMany(o => o.Requests)
                    .SelectMany(r => r.Complaints)
                    .Where(c => c.Status == ComplaintStatus.Disputed)
                    .Select(c => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt, EndDateTime = c.Request.Order.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = c.Request.Order.OrderId,
                        DefaultItemTab = "complaint",
                        InfoDate = c.AnsweredAt.HasValue ? c.AnsweredAt.Value.DateTime : c.CreatedAt.DateTime,
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ButtonItemId = c.Request.OrderId,
                        Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name,
                        OrderNumber = c.Request.Order.OrderNumber,
                        Status = StartListItemStatus.ComplaintEvent,
                        ButtonAction = "View",
                        ButtonController = "Order",
                        ButtonItemTab = "complaint",
                        OrderGroupNumber = c.Request.Order.OrderGroupId.HasValue ? $"Del av {c.Request.Order.Group.OrderGroupNumber}" : string.Empty
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
                sentOrders = _dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, false, false)
                    .Where(o => (o.Status == OrderStatus.Requested)
                    && o.EndAt > _clock.SwedenNow)
                    .Select(o => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Order",
                        DefaultItemId = o.OrderId,
                        InfoDate = o.CreatedAt.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        Language = o.OtherLanguage ?? o.Language.Name,
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
                if (_options.EnableOrderGroups && _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == customerOrganisationId && c.UseOrderGroups))
                {
                    sentOrders.AddRange(_dbContext.OrderGroups.CustomerOrderGroups(customerOrganisationId, userId, customerUnits)
                    .Include(og => og.Orders).ThenInclude(o => o.Language)
                    .Where(og => og.Status == OrderStatus.Requested && og.Orders.Any(o => o.EndAt > _clock.SwedenNow))
                    .ToList()
                    .Select(og => new StartListItemModel
                    {
                        Orderdate = og.Orders.Where(o => o.Status == OrderStatus.Requested)
                            .OrderBy(o => o.StartAt).Select(o =>
                                new TimeRange
                                {
                                    StartDateTime = o.StartAt,
                                    EndDateTime = o.EndAt
                                }).First(),
                        DefaulListAction = "View",
                        DefaulListController = "OrderGroup",
                        DefaultItemId = og.OrderGroupId,
                        InfoDate = og.CreatedAt.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter,
                        Language = og.Orders.Where(o => o.Status == OrderStatus.Requested)
                            .OrderBy(v => v.StartAt)
                            .Select(o => o.OtherLanguage ?? o.Language.Name).FirstOrDefault(),
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
                approvedOrders = _dbContext.Orders.CustomerOrders(customerOrganisationId, userId, customerUnits, false, false, true)
                    .Where(o => o.Status == OrderStatus.ResponseAccepted && o.EndAt > _clock.SwedenNow)
                .Select(o => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Order",
                    DefaultItemId = o.OrderId,
                    InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime,
                    CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel :
                        CompetenceAndSpecialistLevel.NoInterpreter,
                    Language = o.OtherLanguage ?? o.Language.Name,
                    OrderNumber = o.OrderNumber,
                    Status = StartListItemStatus.OrderApproved,
                    OrderGroupNumber = o.OrderGroupId.HasValue ? $"Del av {o.Group.OrderGroupNumber}" : string.Empty
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
            // Awaiting requisition (removed 2019-05-28, requisitions are not in use yet)
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

        private static DateTimeOffset? GetInfoDateForCustomer(Order o)
        {
            return o.Status == OrderStatus.CancelledByBroker ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CancelledAt
                //if status is NoBrokerAcceptedOrder check if last request is answered (denied/declined) else take expiresAt (no answer)
                : o.Status == OrderStatus.NoBrokerAcceptedOrder ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate ?? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().ExpiresAt
                : o.Status == OrderStatus.AwaitingDeadlineFromCustomer ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CreatedAt
                : o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate;
        }

        private static DateTimeOffset? GetInfoDateForCustomer(OrderGroup og)
        {
            switch (og.Status)
            {
                case OrderStatus.RequestAwaitingPartialAccept:
                    return og.RequestGroups.Where(rg => rg.Status == RequestStatus.PartiallyAccepted).FirstOrDefault().AnswerDate;
                case OrderStatus.NoBrokerAcceptedOrder:
                    return og.RequestGroups.OrderByDescending(r => r.RequestGroupId).FirstOrDefault().AnswerDate ?? og.RequestGroups.OrderByDescending(r => r.RequestGroupId).FirstOrDefault().ExpiresAt;
                case OrderStatus.AwaitingDeadlineFromCustomer:
                    return og.RequestGroups.OrderByDescending(r => r.RequestGroupId).FirstOrDefault().CreatedAt;
                default:
                    return og.RequestGroups.OrderByDescending(r => r.RequestGroupId).FirstOrDefault().AnswerDate;
            }
        }

        private IEnumerable<StartList> GetBrokerStartLists()
        {
            var brokerId = User.GetBrokerId();
            var userId = User.GetUserId();
            var actionList = new List<StartListItemModel>();
            //TODO: SHOULD PROBABLY ONLY GET THE USERS THAT ARE ACCTUALLY VIEWING...
            var allOtherUsersInBroker = _dbContext.Users.Where(u => u.Id != userId && u.BrokerId == brokerId && u.IsActive && !u.IsApiUser)
                .Select(u => new
                {
                    Name = u.NameFirst + " " + u.NameFamily,
                    u.Id
                }).ToList();
            //Requests with status received, created
            try
            {
                actionList.AddRange(_dbContext.Requests
                .Where(r => r.RequestGroupId == null &&
                    (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) &&
                    r.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                    DefaulListAction = "Process",
                    DefaulListController = "Request",
                    DefaultItemId = r.RequestId,
                    InfoDate = r.RequestUpdateLatestAnswerTime != null ? r.RequestUpdateLatestAnswerTime.UpdatedAt.DateTime : GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    ButtonItemId = r.RequestId,
                    Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                    OrderNumber = r.Order.OrderNumber,
                    Status = GetStartListStatusForBroker(r.Status, r.Order.ReplacingOrderId ?? 0, false),
                    ButtonAction = "Process",
                    ButtonController = "Request",
                    LatestDate = r.ExpiresAt.HasValue ? (DateTime?)r.ExpiresAt.Value.DateTime : null,
                    ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests with status received, created in method {nameof(GetBrokerStartLists)}");
            }
            //Denied requests 
            try
            {
                actionList.AddRange(_dbContext.Requests
                .Where(r => (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroup.Status != RequestStatus.DeniedByCreator)) &&
                    r.Status == RequestStatus.DeniedByCreator && r.Ranking.BrokerId == brokerId &&
                    !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator))
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = r.RequestId,
                    InfoDate = GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    ButtonItemId = r.RequestId,
                    Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                    OrderNumber = r.Order.OrderNumber,
                    Status = GetStartListStatusForBroker(r.Status, r.Order.ReplacingOrderId ?? 0, false),
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Denied requests in method {nameof(GetBrokerStartLists)}");
            }
            //Requests with status CancelledByCreatorWhenApproved
            try
            {
                actionList.AddRange(_dbContext.Requests
                .Where(r => r.Status == RequestStatus.CancelledByCreatorWhenApproved &&
                    r.Ranking.BrokerId == brokerId &&
                    !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved)
                )
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                    DefaulListAction = r.IsToBeProcessedByBroker ? "Process" : "View",
                    DefaulListController = "Request",
                    DefaultItemId = r.RequestId,
                    InfoDate = GetInfoDateForBroker(r).Value,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    ButtonItemId = r.RequestId,
                    Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                    OrderNumber = r.Order.OrderNumber,
                    Status = GetStartListStatusForBroker(r.Status, r.Order.ReplacingOrderId ?? 0, false),
                    ButtonAction = r.IsToBeProcessedByBroker ? "Process" : "View",
                    ButtonController = "Request",
                    LatestDate = r.IsToBeProcessedByBroker ? (r.ExpiresAt.HasValue ? (DateTime?)r.ExpiresAt.Value.DateTime : null) : null,
                    ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                }).ToList());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests with status CancelledByCreatorWhenApproved in method {nameof(GetBrokerStartLists)}");
            }
            //Non answered responded requests 
            try
            {
                actionList.AddRange(_dbContext.Requests
                .Include(r => r.Order)
                .Where(r => (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroup.Status != RequestStatus.ResponseNotAnsweredByCreator)) && r.Status == RequestStatus.ResponseNotAnsweredByCreator &&
                    r.Ranking.BrokerId == brokerId &&
                    !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator)
                )
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                    DefaulListAction = r.IsToBeProcessedByBroker ? "Process" : "View",
                    DefaulListController = "Request",
                    DefaultItemId = r.RequestId,
                    InfoDate = r.LatestAnswerTimeForCustomer.HasValue ? r.LatestAnswerTimeForCustomer.Value.DateTime : r.Order.StartAt.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    ButtonItemId = r.RequestId,
                    Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                    OrderNumber = r.Order.OrderNumber,
                    Status = GetStartListStatusForBroker(r.Status, r.Order.ReplacingOrderId ?? 0, false),
                    ButtonAction = r.IsToBeProcessedByBroker ? "Process" : "View",
                    ButtonController = "Request",
                    LatestDate = r.IsToBeProcessedByBroker ? (r.ExpiresAt.HasValue ? (DateTime?)r.ExpiresAt.Value.DateTime : null) : null,
                    ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Non answered responded requests in method {nameof(GetBrokerStartLists)}");
            }
            //Non confirmed order changes
            try
            {
                actionList.AddRange(_dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Group)
                .Include(r => r.RequestViews)
                .Where(r => (r.Status == RequestStatus.Approved || r.Status == RequestStatus.AcceptedNewInterpreterAppointed) &&
                    r.Ranking.BrokerId == brokerId && r.Order.EndAt > _clock.SwedenNow &&
                    r.Order.OrderChangeLogEntries.Any(oc => oc.BrokerId == brokerId && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson && oc.OrderChangeConfirmation == null)).ToList()
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                    DefaulListAction = r.IsToBeProcessedByBroker ? "Process" : "View",
                    DefaulListController = "Request",
                    DefaultItemId = r.RequestId,
                    InfoDate = r.Order.OrderChangeLogEntries.Where(oc => oc.BrokerId == brokerId && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson && oc.OrderChangeConfirmation == null)
                        .OrderBy(oc => oc.OrderChangeLogEntryId).Last().LoggedAt.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    ButtonItemId = r.RequestId,
                    Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                    OrderNumber = r.Order.OrderNumber,
                    Status = StartListItemStatus.OrderChanged,
                    ButtonAction = r.IsToBeProcessedByBroker ? "Process" : "View",
                    ButtonController = "Request",
                    ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault()?.ViewedBy,
                    OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Non confirmed order changes in method {nameof(GetBrokerStartLists)}");
            }
            //Requestgroups status received, created, denied, not answered by customer
            try
            {
                actionList.AddRange(_dbContext.RequestGroups
                .Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received || r.Status == RequestStatus.DeniedByCreator || r.Status == RequestStatus.ResponseNotAnsweredByCreator) &&
                    r.Ranking.BrokerId == brokerId &&
                    !r.StatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator || rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator))
                .Select(r => new StartListItemModel
                {
                    Orderdate = r.OrderGroup.Orders.OrderBy(v => v.StartAt).Select(o => new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }).FirstOrDefault(),
                    DefaulListAction = "View",
                    DefaulListController = "RequestGroup",
                    DefaultItemId = r.RequestGroupId,
                    InfoDate = r.Status == RequestStatus.ResponseNotAnsweredByCreator ? (r.LatestAnswerTimeForCustomer.HasValue ? r.LatestAnswerTimeForCustomer.Value.DateTime :
                        r.OrderGroup.Orders.OrderBy(v => v.StartAt).First().StartAt.DateTime) : (r.Status != RequestStatus.DeniedByCreator && r.RequestGroupUpdateLatestAnswerTime != null) ? r.RequestGroupUpdateLatestAnswerTime.UpdatedAt.DateTime : GetInfoDateForBroker(r).Value,
                    CompetenceLevel = r.Status == RequestStatus.DeniedByCreator ?
                        (CompetenceAndSpecialistLevel?)r.Requests.Where(req => req.Status == RequestStatus.DeniedByCreator && !req.Order.IsExtraInterpreterForOrderId.HasValue).First().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter,
                    ExtraCompetenceLevel = !r.OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId.HasValue) ? CompetenceAndSpecialistLevel.NoInterpreter :
                    r.Status == RequestStatus.DeniedByCreator ?
                        (CompetenceAndSpecialistLevel?)r.Requests.Where(req => req.Status == RequestStatus.DeniedByCreator && req.Order.IsExtraInterpreterForOrderId.HasValue).First().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.OrderGroup.Orders.OrderBy(v => v.StartAt).FirstOrDefault().CustomerOrganisation.Name,
                    ButtonItemId = r.RequestGroupId,
                    Language = r.OrderGroup.Orders.OrderBy(v => v.StartAt).Select(o => o.OtherLanguage ?? o.Language.Name).FirstOrDefault(),
                    OrderNumber = r.OrderGroup.OrderGroupNumber,
                    Status = GetStartListStatusForBroker(r.Status, 0, true),
                    ButtonAction = "View",
                    ButtonController = "RequestGroup",
                    LatestDate = r.IsToBeProcessedByBroker ? (r.ExpiresAt.HasValue ? (DateTime?)r.ExpiresAt.Value.DateTime : null) : null,
                    ViewedBy = r.Views.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    IsSingleOccasion = r.OrderGroup.Orders.Count <= 2 && r.OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId.HasValue),
                    HasExtraInterpreter = r.OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId.HasValue)
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requestgroups status received, created, denied, not answered by customer in method {nameof(GetBrokerStartLists)}");
            }
            //Complaints
            try
            {
                actionList.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Created && c.Request.Ranking.BrokerId == brokerId)
                .Select(c => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt, EndDateTime = c.Request.Order.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = c.Request.RequestId,
                    DefaultItemTab = "complaint",
                    InfoDate = c.CreatedAt.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = c.Request.Order.CustomerOrganisation.Name,
                    ButtonItemId = c.RequestId,
                    Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name,
                    OrderNumber = c.Request.Order.OrderNumber,
                    Status = StartListItemStatus.ComplaintEvent,
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ButtonItemTab = "complaint",
                    ViewedBy = c.Request.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    OrderGroupNumber = c.Request.Order.OrderGroupId.HasValue ? $"Del av {c.Request.Order.Group.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Complaints in method {nameof(GetBrokerStartLists)}");
            }
            //Requests to be reported
            try
            {
                actionList.AddRange(_dbContext.Requests
                .Where(r => r.Status == RequestStatus.Approved && r.Order.StartAt < _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                 .Select(r => new StartListItemModel
                 {
                     Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                     DefaulListAction = "View",
                     DefaulListController = "Request",
                     DefaultItemId = r.RequestId,
                     InfoDate = r.Order.EndAt.DateTime,
                     InfoDateDescription = "Utfört: ",
                     CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                     CustomerName = r.Order.CustomerOrganisation.Name,
                     ButtonItemId = r.RequestId,
                     Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                     OrderNumber = r.Order.OrderNumber,
                     Status = StartListItemStatus.RequisitionToBeCreated,
                     ButtonAction = "View",
                     ButtonController = "Request",
                     ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                     OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                 }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Requests to be reported in method {nameof(GetBrokerStartLists)}");
            }
            //Commented requisitions
            try
            {
                actionList.AddRange(_dbContext.Requisitions
                .Where(r => r.Request.Ranking.BrokerId == brokerId && !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.Commented)
                .Select(r => new StartListItemModel
                {
                    Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt },
                    DefaulListAction = "View",
                    DefaulListController = "Request",
                    DefaultItemId = r.RequestId,
                    DefaultItemTab = "requisition",
                    InfoDate = r.ProcessedAt.Value.DateTime,
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.Request.Order.CustomerOrganisation.Name,
                    ButtonItemId = r.RequestId,
                    Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name,
                    OrderNumber = r.Request.Order.OrderNumber,
                    Status = StartListItemStatus.RequisitionCommented,
                    ButtonAction = "View",
                    ButtonController = "Request",
                    ButtonItemTab = "requisition",
                    ViewedBy = r.Request.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    OrderGroupNumber = r.Request.Order.OrderGroupId.HasValue ? $"Del av {r.Request.Order.Group.OrderGroupNumber}" : string.Empty
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Commented requisitions in method {nameof(GetBrokerStartLists)}");
            }
            var count = actionList.Any() ? actionList.Count : 0;
            actionList.ForEach(l => l.ViewedByUser = l.ViewedBy.HasValue && l.ViewedBy != userId ? allOtherUsersInBroker.Single(a => a.Id == l.ViewedBy).Name + " håller på med detta ärende" : string.Empty);
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
                acceptedRequests = _dbContext.Requests
                    .Where(r => ((r.RequestGroupId == null & r.Status == RequestStatus.Accepted) || r.Status == RequestStatus.AcceptedNewInterpreterAppointed) &&
                        r.Order.StartAt > _clock.SwedenNow && r.Ranking.BrokerId == brokerId)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Request",
                        DefaultItemId = r.RequestId,
                        InfoDate = r.AnswerDate.Value.DateTime,
                        InfoDateDescription = "Tillsatt: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.Order.CustomerOrganisation.Name,
                        Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                        OrderNumber = r.Order.OrderNumber,
                        Status = r.Status == RequestStatus.AcceptedNewInterpreterAppointed ? StartListItemStatus.NewInterpreterForApproval : StartListItemStatus.OrderAcceptedForApproval,
                        ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                        OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted requests in method {nameof(GetBrokerStartLists)}");
            }
            try
            {
                acceptedRequests.AddRange(_dbContext.RequestGroups
                .Where(r => r.Status == RequestStatus.Accepted && !r.OrderGroup.Orders.Any(o => o.StartAt < _clock.SwedenNow) && r.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel
                {
                    Orderdate = r.OrderGroup.Orders.OrderBy(v => v.StartAt).Select(o => new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }).FirstOrDefault(),
                    DefaulListAction = "View",
                    DefaulListController = "RequestGroup",
                    DefaultItemId = r.RequestGroupId,
                    InfoDate = r.AnswerDate.Value.DateTime,
                    InfoDateDescription = "Tillsatt: ",
                    CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Requests.Where(req => req.Status == RequestStatus.Accepted && !req.Order.IsExtraInterpreterForOrderId.HasValue).First().CompetenceLevel,
                    ExtraCompetenceLevel = r.OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId.HasValue) ?
                        (CompetenceAndSpecialistLevel?)r.Requests.Where(req => req.Status == RequestStatus.Accepted && req.Order.IsExtraInterpreterForOrderId.HasValue).First().CompetenceLevel :
                        CompetenceAndSpecialistLevel.NoInterpreter,
                    CustomerName = r.OrderGroup.Orders.OrderBy(v => v.StartAt).FirstOrDefault().CustomerOrganisation.Name,
                    Language = r.OrderGroup.Orders.OrderBy(v => v.StartAt).Select(o => o.OtherLanguage ?? o.Language.Name).FirstOrDefault(),
                    OrderNumber = r.OrderGroup.OrderGroupNumber,
                    Status = StartListItemStatus.OrderGroupAwaitingApproval,
                    ViewedBy = r.Views.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                    IsSingleOccasion = r.OrderGroup.Orders.Count <= 2 && r.OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId.HasValue),
                    HasExtraInterpreter = r.OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId.HasValue)
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Accepted requestgroups is changed in method {nameof(GetBrokerStartLists)}");
            }
            count = acceptedRequests.Any() ? acceptedRequests.Count : 0;
            acceptedRequests.ForEach(l => l.ViewedByUser = l.ViewedBy.HasValue && l.ViewedBy != userId ? allOtherUsersInBroker.Single(a => a.Id == l.ViewedBy).Name + " håller på med detta ärende" : string.Empty);

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
                approvedRequestAnswers = _dbContext.Requests
                    .Where(r => r.Status == RequestStatus.Approved && r.Order.StartAt > _clock.SwedenNow && r.Ranking.BrokerId == brokerId)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Request",
                        DefaultItemId = r.RequestId,
                        InfoDate = (r.Status == RequestStatus.Approved && r.AnswerProcessedAt.HasValue) ? r.AnswerProcessedAt.Value.DateTime : r.AnswerDate.Value.DateTime,
                        InfoDateDescription = "Godkänd: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.Order.CustomerOrganisation.Name,
                        Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                        OrderNumber = r.Order.OrderNumber,
                        Status = StartListItemStatus.OrderApproved,
                        ViewedBy = r.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                        OrderGroupNumber = r.Order.OrderGroupId.HasValue ? $"Del av {r.Order.Group.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Approved requests in method {nameof(GetBrokerStartLists)}");
            }
            count = approvedRequestAnswers.Any() ? approvedRequestAnswers.Count : 0;
            approvedRequestAnswers.ForEach(l => l.ViewedByUser = l.ViewedBy.HasValue && l.ViewedBy != userId ? allOtherUsersInBroker.Single(a => a.Id == l.ViewedBy).Name + " håller på med detta ärende" : string.Empty);

            yield return new StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = approvedRequestAnswers,
                DisplayCustomer = true
            };

            //Sent requisitions
            List<StartListItemModel> sentRequisitions = new List<StartListItemModel>();
            try
            {
                sentRequisitions = _dbContext.Requisitions
                    .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.Created && r.Request.Ranking.BrokerId == brokerId)
                    .Select(r => new StartListItemModel
                    {
                        Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt },
                        DefaulListAction = "View",
                        DefaulListController = "Request",
                        DefaultItemId = r.RequestId,
                        InfoDate = r.CreatedAt.DateTime,
                        InfoDateDescription = "Skickad: ",
                        CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        CustomerName = r.Request.Order.CustomerOrganisation.Name,
                        Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name,
                        OrderNumber = r.Request.Order.OrderNumber,
                        Status = StartListItemStatus.RequisitionCreated,
                        ViewedBy = r.Request.RequestViews.OrderBy(v => v.ViewedAt).FirstOrDefault().ViewedBy,
                        OrderGroupNumber = r.Request.Order.OrderGroupId.HasValue ? $"Del av {r.Request.Order.Group.OrderGroupNumber}" : string.Empty
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occured for Sent requisitions in method {nameof(GetBrokerStartLists)}");
            }
            count = sentRequisitions.Any() ? sentRequisitions.Count : 0;
            sentRequisitions.ForEach(l => l.ViewedByUser = l.ViewedBy.HasValue && l.ViewedBy != userId ? allOtherUsersInBroker.Single(a => a.Id == l.ViewedBy).Name + " håller på med detta ärende" : string.Empty);

            yield return new StartList
            {
                Header = count > 0 ? $"Skickade rekvisitioner ({count} st)" : "Skickade rekvisitioner",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar med skickad rekvisition",
                StartListObjects = sentRequisitions,
                DisplayCustomer = true
            };
        }

        private static DateTime? GetInfoDateForBroker(Request r)
        {
            return (r.Status == RequestStatus.CancelledByCreator || r.Status == RequestStatus.CancelledByCreatorWhenApproved) ? r.CancelledAt?.DateTime : r.Status == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.CreatedAt.DateTime;
        }

        private static DateTime? GetInfoDateForBroker(RequestGroup r)
        {
            return r.Status == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.CreatedAt.DateTime;
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
