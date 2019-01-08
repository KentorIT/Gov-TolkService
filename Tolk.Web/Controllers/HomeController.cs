using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Helpers;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Tolk.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;

        public HomeController(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            ISwedishClock clock,
            IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _clock = clock;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index(string message)
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
                PageTitle = User.IsInRole(Roles.Admin) ? "Startsida för tolkavropstjänsten" : "Aktiva bokningar",
                Message = message,
                ConfirmationMessages = GetConfirmationMessages(),
                StartLists = await GetStartLists()
            });
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

            //Accepted orders to approve, Cancelled by broker, Non-answered-orders
            actionList.AddRange(_dbContext.Orders.Include(o => o.Requests).ThenInclude(r => r.RequestStatusConfirmations)
                .Include(o => o.OrderStatusConfirmations)
                .Include(o => o.Language).Where(o => (o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.RequestRespondedNewInterpreter || o.Status == OrderStatus.NoBrokerAcceptedOrder || o.Status == OrderStatus.CancelledByBroker)
                && o.CreatedBy == User.GetUserId() && !o.OrderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder) && !o.Requests.OrderByDescending(r => r.RequestId).First().RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker))
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = GetInfoDateForCustomer(o).Value, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = GetStartListStatusForCustomer(o.Status), ButtonAction = "View", ButtonController = "Order" }).ToList());

            //Requisitions to review (for user and where user is contact person)
            actionList.AddRange(_dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Where(r => r.Status == RequisitionStatus.Created && r.Request.Order.Status == OrderStatus.Delivered &&
                (r.Request.Order.CreatedBy == User.GetUserId() || r.Request.Order.ContactPersonId == User.GetUserId()))
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt.DateTime, EndDateTime = r.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = r.Request.Order.OrderId, DefaultItemTab = "requisition", InfoDate = r.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = r.Request.OrderId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitonArrived, ButtonAction = "View", ButtonController = "Order", ButtonItemTab = "requisition" }).ToList());

            //Disputed complaints
            actionList.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Disputed &&
                c.CreatedBy == User.GetUserId()).Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Select(c => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt.DateTime, EndDateTime = c.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = c.Request.Order.OrderId, DefaultItemTab = "complaint", InfoDate = c.AnsweredAt.HasValue ? c.AnsweredAt.Value.DateTime : c.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = c.Request.OrderId, Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name, OrderNumber = c.Request.Order.OrderNumber, Status = StartListItemStatus.ComplaintEvent, ButtonAction = "View", ButtonController = "Order", ButtonItemTab = "complaint" }).ToList());

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av myndighet ({count} st)" : "Kräver handling av myndighet",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som kräver handling av myndigheten",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            //Sent orders
            var sentOrders = _dbContext.Orders
                .Include(o => o.Language).
                Where(o => o.Status == OrderStatus.Requested &&
            o.CreatedBy == User.GetUserId() && o.EndAt > _clock.SwedenNow).Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.CreatedAt.DateTime, InfoDateDescription = "Skickad: ", CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderCreated }).ToList();

            count = sentOrders.Any() ? sentOrders.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Skickade bokningar ({count} st)" : "Skickade bokningsförfrågningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är skickade",
                StartListObjects = sentOrders
            };

            //Approved orders 
            var approvedOrders = _dbContext.Orders.Include(o => o.Requests)
            .Include(o => o.Language).Where(o => o.Status == OrderStatus.ResponseAccepted &&
            o.CreatedBy == User.GetUserId() && o.EndAt > _clock.SwedenNow).Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderApproved }).ToList();

            count = approvedOrders.Any() ? approvedOrders.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = approvedOrders
            };

            // Awaiting requisition
            var awaitRequisition = _dbContext.Orders.Include(o => o.Requests)
            .Include(o => o.Language).Where(o => o.Status == OrderStatus.ResponseAccepted &&
            o.CreatedBy == User.GetUserId() && o.EndAt < _clock.SwedenNow && !(o.Requests.Any(r => r.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.AutomaticApprovalFromCancelledOrder || req.Status == RequisitionStatus.Created)))).Select
            (o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.RequisitionAwaited }).ToList();

            count = awaitRequisition.Any() ? awaitRequisition.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Inväntar rekvisition ({count} st)" : "Inväntar rekvisition",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som inväntar rekvisition",
                StartListObjects = awaitRequisition
            };
        }

        private StartListItemStatus GetStartListStatusForCustomer(OrderStatus status)
        {
            return status == OrderStatus.CancelledByBroker ? StartListItemStatus.OrderCancelled : status == OrderStatus.NoBrokerAcceptedOrder ? StartListItemStatus.OrderNotAnswered : StartListItemStatus.OrderAcceptedForApproval;
        }

        private DateTime? GetInfoDateForCustomer(Order o)
        {
            return o.Status == OrderStatus.CancelledByBroker ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CancelledAt.Value.DateTime : o.Status == OrderStatus.NoBrokerAcceptedOrder ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().ExpiresAt.DateTime : o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime;
        }

        private IEnumerable<StartViewModel.StartList> GetBrokerStartLists()
        {
            var brokerId = User.GetBrokerId();
            var actionList = new List<StartListItemModel>();

            //requests with status received, created, denied, cancelled by customer
            actionList.AddRange(_dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.RequestStatusConfirmations)
                .Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received || r.Status == RequestStatus.CancelledByCreatorWhenApproved || r.Status == RequestStatus.DeniedByCreator) &&
                r.Ranking.BrokerId == brokerId && !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator) && !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved))
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) ? "Process" : "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = GetInfoDateForBroker(r).Value, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = GetStartListStatusForBroker(r.Status, r.Order.ReplacingOrderId ?? 0), ButtonAction = r.Status == RequestStatus.Created || r.Status == RequestStatus.Received ? "Process" : "View", ButtonController = "Request", LatestDate = r.Status == RequestStatus.Created || r.Status == RequestStatus.Received ? (DateTime?)r.ExpiresAt.DateTime : null }).ToList());

            //Complaints
            actionList.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Created && c.Request.Ranking.BrokerId == brokerId)
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Select(c => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt.DateTime, EndDateTime = c.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = c.Request.RequestId, DefaultItemTab = "complaint", InfoDate = c.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = c.Request.Order.CustomerOrganisation.Name, ButtonItemId = c.RequestId, Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name, OrderNumber = c.Request.Order.OrderNumber, Status = StartListItemStatus.ComplaintEvent, ButtonAction = "View", ButtonController = "Request", ButtonItemTab = "complaint" }).ToList());

            //To be reported
            actionList.AddRange(_dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => r.Status == RequestStatus.Approved && r.Order.StartAt < _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                 .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.Order.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = StartListItemStatus.RequisitionToBeCreated, ButtonAction = "Create", ButtonController = "Requisition" }).ToList());

            //Denied requisitions
            actionList.AddRange(_dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.DeniedByCustomer &&
                !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) && r.Request.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, DefaultItemTab = "requisition", InfoDate = r.ProcessedAt.Value.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionDenied, ButtonAction = "View", ButtonController = "Request", ButtonItemTab = "requisition" }).ToList());

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av förmedling ({count} st)" : "Kräver handling av förmedling",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som kräver handling av förmedling",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            //approved and accepted (not approved but answered) requests 
            var answeredRequests = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => (r.Status == RequestStatus.Approved || r.Status == RequestStatus.AcceptedNewInterpreterAppointed || r.Status == RequestStatus.Accepted) && r.Order.StartAt > _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = (r.Status == RequestStatus.Approved && r.AnswerProcessedAt.HasValue) ? r.AnswerProcessedAt.Value.DateTime : r.AnswerDate.Value.DateTime, InfoDateDescription = r.Status == RequestStatus.Approved ? "Godkänd: ": "Tillsatt: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = r.Status == RequestStatus.Approved ? StartListItemStatus.OrderApproved : StartListItemStatus.OrderAcceptedForApproval }).ToList();

            count = answeredRequests.Any() ? answeredRequests.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar som är tillsatta",
                StartListObjects = answeredRequests
            };

            //sent requisitions
            var sentRequisitions = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.Created && r.Request.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.Request.Order.EndAt.DateTime, InfoDateDescription = "Skickad: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionCreated }).ToList();

            count = sentRequisitions.Any() ? sentRequisitions.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Skickade rekvisitioner ({count} st)" : "Skickade rekvisitioner",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningar med skickad rekvisition",
                StartListObjects = sentRequisitions
            };
        }

        private DateTime? GetInfoDateForBroker(Request r)
        {
            return r.Status == RequestStatus.CancelledByCreator ? r.CancelledAt?.DateTime : r.Status == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.CreatedAt.DateTime;
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

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policies.TimeTravel)]
        public IActionResult TimeTravel(DateTime date, TimeSpan time, string action)
        {
            var clock = (TimeTravelClock)_clock;

            switch (action)
            {
                case "Jump":
                    var targetDateTime = date.Add(time).ToDateTimeOffsetSweden();
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
