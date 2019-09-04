using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
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

        public HomeController(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            ILogger<HomeController> logger,
            VerificationService verificationService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _clock = clock;
            _authorizationService = authorizationService;
            _logger = logger;
            _verificationService = verificationService;
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
                    var hasPassword = await _userManager.HasPasswordAsync(user);

                    if (!hasPassword)
                    {
                        return RedirectToAction("RegisterNewAccount", "Account");
                    }
                    if (!(await _authorizationService.AuthorizeAsync(User, Policies.ViewMenuAndStartLists)).Succeeded)
                    {
                        return RedirectToAction("Edit", "Account");
                    }
                }
            }
            return View(new StartViewModel
            {
                PageTitle = (User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator)) ? "Startsida för tolkavropstjänsten" : "Aktiva bokningar",
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

        private async Task<IEnumerable<StartViewModel.StartList>> GetStartLists()
        {
            var result = Enumerable.Empty<StartViewModel.StartList>();

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

        private IEnumerable<StartViewModel.StartList> GetCustomerStartLists()
        {
            var actionList = new List<StartListItemModel>();
            var userId = User.GetUserId();
            var customerOrganisationId = User.GetCustomerOrganisationId();
            var customerUnits = User.TryGetAllCustomerUnits();
            //Accepted orders to approve, Cancelled by broker, Non-answered-orders
            var ordersCorrectStatusAndUser = _dbContext.Orders.Include(o => o.Requests).Include(o => o.Language)
                .Where(o => (o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.RequestRespondedNewInterpreter
                || o.Status == OrderStatus.NoBrokerAcceptedOrder || o.Status == OrderStatus.CancelledByBroker || o.Status == OrderStatus.AwaitingDeadlineFromCustomer)
                && o.IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, false)).ToList();

            actionList.AddRange(ordersCorrectStatusAndUser
                .Where(os => !_dbContext.RequestStatusConfirmation.Where(rs => rs.RequestStatus == RequestStatus.CancelledByBroker)
                .Select(rs => rs.RequestId).Contains(os.Requests.OrderByDescending(r => r.RequestId).First().RequestId) &&
                !_dbContext.OrderStatusConfirmation.Where(osc => osc.OrderStatus == OrderStatus.NoBrokerAcceptedOrder)
                .Select(osc => osc.OrderId).Contains(os.OrderId))
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
                    ButtonAction = "View",
                    ButtonController = "Order"
                }));

            //Requisitions to review
            actionList.AddRange(_dbContext.Requisitions
                .Where(r => r.Status == RequisitionStatus.Created && r.Request.Order.Status == OrderStatus.Delivered &&
                    r.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, customerOrganisationId, userId, false))
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
                    ButtonItemTab = "requisition"
                }));

            //Disputed complaints
            actionList.AddRange(_dbContext.Complaints
                .Where(c => c.Status == ComplaintStatus.Disputed &&
                c.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, customerOrganisationId, userId, false))
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
                    ButtonItemTab = "complaint"
                }));

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av myndighet ({count} st)" : "Kräver handling av myndighet",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som kräver handling av myndigheten",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            //Sent and approved orders
            var sentAndApprovedOrders = _dbContext.Orders.Include(o => o.Requests).Include(o => o.Language)
                .Where(o => (o.Status == OrderStatus.Requested || o.Status == OrderStatus.ResponseAccepted)
                && o.IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, false)
                && o.EndAt > _clock.SwedenNow).ToList();

            //Sent orders
            var sentOrders = sentAndApprovedOrders.Where(o => o.Status == OrderStatus.Requested)
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
                });

            count = sentOrders.Any() ? sentOrders.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Skickade bokningar ({count} st)" : "Skickade bokningsförfrågningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är skickade",
                StartListObjects = sentOrders
            };

            //Approved orders 
            var approvedOrders = sentAndApprovedOrders.Where(o => o.Status == OrderStatus.ResponseAccepted)
            .Select(o => new StartListItemModel
            {
                Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt },
                DefaulListAction = "View",
                DefaulListController = "Order",
                DefaultItemId = o.OrderId,
                InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime,
                CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter,
                Language = o.OtherLanguage ?? o.Language.Name,
                OrderNumber = o.OrderNumber,
                Status = StartListItemStatus.OrderApproved
            });

            count = approvedOrders.Any() ? approvedOrders.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = approvedOrders
            };

            // Awaiting requisition
            //Was removed 2019-05-28 because the requisitions are not in us yet.
        }

        private StartListItemStatus GetStartListStatusForCustomer(OrderStatus status, int replacingOrderId)
        {
            return status == OrderStatus.CancelledByBroker ? StartListItemStatus.OrderCancelled
                : (status == OrderStatus.NoBrokerAcceptedOrder && replacingOrderId > 0) ? StartListItemStatus.ReplacementOrderNotAnswered
                : (status == OrderStatus.NoBrokerAcceptedOrder && replacingOrderId == 0) ? StartListItemStatus.OrderNotAnswered
                : status == OrderStatus.RequestRespondedNewInterpreter ? StartListItemStatus.NewInterpreterForApproval
                : status == OrderStatus.AwaitingDeadlineFromCustomer ? StartListItemStatus.AwaitingDeadlineFromCustomer
                : StartListItemStatus.OrderAcceptedForApproval;
        }

        private DateTimeOffset? GetInfoDateForCustomer(Order o)
        {
            return o.Status == OrderStatus.CancelledByBroker ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CancelledAt
                //if status is NoBrokerAcceptedOrder check if last request is answered (denied/declined) else take expiresAt (no answer)
                : o.Status == OrderStatus.NoBrokerAcceptedOrder ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate ?? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().ExpiresAt
                : o.Status == OrderStatus.AwaitingDeadlineFromCustomer ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CreatedAt
                : o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate;
        }

        private IEnumerable<StartViewModel.StartList> GetBrokerStartLists()
        {
            var brokerId = User.GetBrokerId();
            var userId = User.GetUserId();
            var actionList = new List<StartListItemModel>();

            //requests with status received, created, denied, cancelled by customer
            actionList.AddRange(_dbContext.Requests
                .Where(r => (r.IsToBeProcessedByBroker || r.Status == RequestStatus.CancelledByCreatorWhenApproved || r.Status == RequestStatus.DeniedByCreator) &&
                r.Ranking.BrokerId == brokerId &&
                !_dbContext.RequestStatusConfirmation.Where(rs => rs.RequestStatus == RequestStatus.DeniedByCreator || rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved)
                .Select(rs => rs.RequestId).Contains(r.RequestId))
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = r.IsToBeProcessedByBroker ? "Process" : "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = GetInfoDateForBroker(r).Value, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = GetStartListStatusForBroker(r.Status, r.Order.ReplacingOrderId ?? 0), ButtonAction = r.IsToBeProcessedByBroker ? "Process" : "View", ButtonController = "Request", LatestDate = r.IsToBeProcessedByBroker ? (DateTime?)r.ExpiresAt.Value.DateTime : null, ViewedByUser = GetViewedByForBroker(r.RequestViews.FirstOrDefault(rv => rv.ViewedBy != userId).ViewedByUser) }).ToList());

            //Complaints
            actionList.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Created && c.Request.Ranking.BrokerId == brokerId)
                .Select(c => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt, EndDateTime = c.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = c.Request.RequestId, DefaultItemTab = "complaint", InfoDate = c.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = c.Request.Order.CustomerOrganisation.Name, ButtonItemId = c.RequestId, Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name, OrderNumber = c.Request.Order.OrderNumber, Status = StartListItemStatus.ComplaintEvent, ButtonAction = "View", ButtonController = "Request", ButtonItemTab = "complaint", ViewedByUser = GetViewedByForBroker(c.Request.RequestViews.FirstOrDefault(rv => rv.ViewedBy != userId).ViewedByUser) }).ToList());

            //To be reported
            actionList.AddRange(_dbContext.Requests
                .Where(r => r.Status == RequestStatus.Approved && r.Order.StartAt < _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                 .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.Order.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = StartListItemStatus.RequisitionToBeCreated, ButtonAction = "Create", ButtonController = "Requisition", ViewedByUser = GetViewedByForBroker(r.RequestViews.FirstOrDefault(rv => rv.ViewedBy != userId).ViewedByUser) }).ToList());

            //Commented requisitions
            actionList.AddRange(_dbContext.Requisitions
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.Commented &&
                !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Reviewed || req.Status == RequisitionStatus.Created) && r.Request.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, DefaultItemTab = "requisition", InfoDate = r.ProcessedAt.Value.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionCommented, ButtonAction = "View", ButtonController = "Request", ButtonItemTab = "requisition", ViewedByUser = GetViewedByForBroker(r.Request.RequestViews.FirstOrDefault(rv => rv.ViewedBy != userId).ViewedByUser) }).ToList());

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av förmedling ({count} st)" : "Kräver handling av förmedling",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som kräver handling av förmedling",
                StartListObjects = actionList,
                HasReviewAction = true,
                DisplayCustomer = true
            };

            //approved and accepted (not approved but answered) requests 
            var answeredRequests = _dbContext.Requests
                .Where(r => r.IsAcceptedOrApproved && r.Order.StartAt > _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = (r.Status == RequestStatus.Approved && r.AnswerProcessedAt.HasValue) ? r.AnswerProcessedAt.Value.DateTime : r.AnswerDate.Value.DateTime, InfoDateDescription = r.Status == RequestStatus.Approved ? "Godkänd: " : "Tillsatt: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = r.Status == RequestStatus.Approved ? StartListItemStatus.OrderApproved : r.Status == RequestStatus.AcceptedNewInterpreterAppointed ? StartListItemStatus.NewInterpreterForApproval : StartListItemStatus.OrderAcceptedForApproval, ViewedByUser = GetViewedByForBroker(r.RequestViews.FirstOrDefault(rv => rv.ViewedBy != userId).ViewedByUser) }).ToList();

            count = answeredRequests.Any() ? answeredRequests.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = answeredRequests,
                DisplayCustomer = true
            };

            //sent requisitions
            var sentRequisitions = _dbContext.Requisitions
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.Created && r.Request.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.CreatedAt.DateTime, InfoDateDescription = "Skickad: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionCreated, ViewedByUser = GetViewedByForBroker(r.Request.RequestViews.FirstOrDefault(rv => rv.ViewedBy != userId).ViewedByUser) }).ToList();

            count = sentRequisitions.Any() ? sentRequisitions.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Skickade rekvisitioner ({count} st)" : "Skickade rekvisitioner",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar med skickad rekvisition",
                StartListObjects = sentRequisitions,
                DisplayCustomer = true
            };
        }

        private string GetViewedByForBroker(AspNetUser viewedByUser)
        {
            return viewedByUser == null ? string.Empty : viewedByUser.FullName + " håller på med detta ärende";
        }

        private DateTime? GetInfoDateForBroker(Request r)
        {
            return (r.Status == RequestStatus.CancelledByCreator || r.Status == RequestStatus.CancelledByCreatorWhenApproved) ? r.CancelledAt?.DateTime : r.Status == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.CreatedAt.DateTime;
        }

        private StartListItemStatus GetStartListStatusForBroker(RequestStatus requestStatus, int replacingOrderId)
        {
            return (requestStatus == RequestStatus.Received && replacingOrderId == 0) ? StartListItemStatus.RequestReceived : (requestStatus == RequestStatus.Received && replacingOrderId > 0) ? StartListItemStatus.ReplacementOrderRequestReceived : (requestStatus == RequestStatus.Created && replacingOrderId == 0) ? StartListItemStatus.RequestArrived : (requestStatus == RequestStatus.Created && replacingOrderId > 0) ? StartListItemStatus.ReplacementOrderRequestArrived : requestStatus == RequestStatus.DeniedByCreator ? StartListItemStatus.RequestDenied : StartListItemStatus.OrderCancelled;
        }

        private IEnumerable<StartViewModel.StartList> GetInterpreterStartLists()
        {
            return new List<StartViewModel.StartList>();
        }

        private IEnumerable<StartViewModel.ConfirmationMessage> GetConfirmationMessages()
        {
            return Enumerable.Empty<StartViewModel.ConfirmationMessage>();
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
    }
}
