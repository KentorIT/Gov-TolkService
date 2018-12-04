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
                    if (!(await _authorizationService.AuthorizeAsync(User, Policies.RenderMenuAndStartPageBoxes)).Succeeded)
                    {
                        return RedirectToAction("Edit", "Account");
                    }
                }
            }
            return View(new StartViewModel
            {
                PageTitle = User.IsInRole(Roles.Admin) ? "Startsida för tolkavropstjänsten" : "Aktiva bokningsförfrågningar",
                Message = message,
                Boxes = await GetStartPageBoxes(),
                ConfirmationMessages = await GetConfirmationMessages(),
                StartLists = await GetStartLists()
            });
        }

        private async Task<IEnumerable<StartViewModel.StartPageBox>> GetStartPageBoxes()
        {
            var result = Enumerable.Empty<StartViewModel.StartPageBox>();

            if ((await _authorizationService.AuthorizeAsync(User, Policies.Broker)).Succeeded)
            {
                result = result.Union(GetBrokerStartPageBoxes());
            }

            if ((await _authorizationService.AuthorizeAsync(User, Policies.Interpreter)).Succeeded)
            {
                result = result.Union(GetInterpreterStartPageBoxes());
            }

            return result;
        }

        private async Task<IEnumerable<StartViewModel.StartList>> GetStartLists()
        {
            var result = Enumerable.Empty<StartViewModel.StartList>();

            if ((await _authorizationService.AuthorizeAsync(User, Policies.Customer)).Succeeded)
            {
                result = result.Union(GetCustomerStartLists());
            }
            return result;
        }

        private IEnumerable<StartViewModel.StartPageBox> GetBrokerStartPageBoxes()
        {
            var brokerId = User.GetBrokerId();

            yield return new StartViewModel.StartPageBox
            {
                Count = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) &&
                    r.Ranking.BrokerId == brokerId).Count(),
                Header = "Inkomna förfrågningar",
                Controller = "Request",
                Action = "List",
                Filters = new Dictionary<string, string> {
                        { "Status", RequestStatus.ToBeProcessedByBroker.ToString() }
                    }

            };

            yield return new StartViewModel.StartPageBox
            {
                Count = _dbContext.Requests.Where(r => r.Status == RequestStatus.Approved &&
                    r.Order.StartAt < _clock.SwedenNow &&
                    !r.Requisitions.Any() &&
                    r.Ranking.BrokerId == brokerId).Count(),
                Header = "Tolktillfällen att avrapportera",
                Controller = "Assignment",
                Action = "List",
                Filters = new Dictionary<string, string> {
                        { "Status", AssignmentStatus.ToBeReported.ToString() }
                    }
            };

            int count = _dbContext.Requisitions.Where(r => !r.ReplacedByRequisitionId.HasValue &&
                    r.Status == RequisitionStatus.DeniedByCustomer &&
                   !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) &&
                   r.Request.Ranking.BrokerId == brokerId).Count();

            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Underkända rekvisitioner",
                    Controller = "Requisition",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", RequisitionStatus.DeniedByCustomer.ToString() }
                    }
                };
            }

            count = _dbContext.Requests.Where(r => r.Status == RequestStatus.CancelledByCreatorWhenApproved && r.Ranking.BrokerId == brokerId).Count();

            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Avbokade förfrågningar från myndighet",
                    Controller = "Request",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", nameof(RequestStatus.CancelledByCreatorWhenApproved) }
                    }

                };
            }

            count = _dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Created &&
                    c.Request.Ranking.BrokerId == brokerId).Count();

            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Inkomna reklamationer",
                    Controller = "Complaint",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", ComplaintStatus.Created.ToString() }
                    }
                };
            }
        }

        private IEnumerable<StartViewModel.StartPageBox> GetInterpreterStartPageBoxes()
        {
            var interpreterId = User.GetInterpreterId();

            yield return new StartViewModel.StartPageBox
            {
                //TODO: Here we need to check the order too! 
                Count = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Approved) &&
                    r.Order.StartAt > _clock.SwedenNow &&
                    !r.Requisitions.Any() &&
                    r.InterpreterId == interpreterId).Count(),
                Header = "Kommande uppdrag",
                Controller = "Assignment",
                Action = "List",
                Filters = new Dictionary<string, string> {
                        { "Status", AssignmentStatus.ToBeExecuted.ToString() }
                    }
            };
            yield return new StartViewModel.StartPageBox
            {
                Count = _dbContext.Requests.Where(r => r.Status == RequestStatus.Approved &&
                    r.Order.StartAt < _clock.SwedenNow &&
                    !r.Requisitions.Any() &&
                    r.InterpreterId == interpreterId).Count(),
                Header = "Att avrapportera",
                Controller = "Assignment",
                Action = "List",
                Filters = new Dictionary<string, string> {
                        { "Status", AssignmentStatus.ToBeReported.ToString() }
                    }
            };
            int count = _dbContext.Requisitions.Where(r => !r.ReplacedByRequisitionId.HasValue &&
                r.Status == RequisitionStatus.DeniedByCustomer &&
                   !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) &&
                  r.Request.InterpreterId == interpreterId).Count();
            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Underkända rekvisitioner",
                    Controller = "Requisition",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", RequisitionStatus.DeniedByCustomer.ToString() }
                    }

                };
            }
        }

        private IEnumerable<StartViewModel.StartList> GetCustomerStartLists()
        {

            var listToAddAll = new List<StartListItemModel>();

            //tillsatt tolk för godkännade
            listToAddAll.AddRange(_dbContext.Orders.Include(o => o.Requests)
                .Include(o => o.Language).Where(o => (o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.RequestRespondedNewInterpreter) && o.CreatedBy == User.GetUserId())
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.Any() ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime : _clock.SwedenNow.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderAcceptedForApproval, ButtonAction = "View", ButtonController = "Order" }).ToList());

            //Requisitions to review att kontrollera (for user and where user is contact person)
            listToAddAll.AddRange(_dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Where(r => r.Status == RequisitionStatus.Created && r.Request.Order.Status == OrderStatus.Delivered &&
                (r.Request.Order.CreatedBy == User.GetUserId() || r.Request.Order.ContactPersonId == User.GetUserId()))
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt.DateTime, EndDateTime = r.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = r.Request.Order.OrderId, InfoDate = r.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = r.RequisitionId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitonArrived, ButtonAction = "Process", ButtonController = "Requisition" }).ToList()); ;

            //Disputed complaints
            listToAddAll.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Disputed &&
                c.CreatedBy == User.GetUserId()).Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Select(c => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt.DateTime, EndDateTime = c.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = c.Request.Order.OrderId, InfoDate = c.AnsweredAt.HasValue ? c.AnsweredAt.Value.DateTime : c.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = c.ComplaintId, Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name, OrderNumber = c.Request.Order.OrderNumber, Status = StartListItemStatus.ComplaintEvent, ButtonAction = "View", ButtonController = "Complaint" }).ToList()); ;

            //Non-answered-requests, is this correct with arrivaldate and check on orderstatus?
            listToAddAll.AddRange(_dbContext.Orders.Include(o => o.Requests)
                .Include(o => o.Language).Where(o => o.Status == OrderStatus.NoBrokerAcceptedOrder && o.CreatedBy == User.GetUserId())
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().ExpiresAt.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderNotAnswered, ButtonAction = "View", ButtonController = "Order" }).ToList());

            //Cancelled by broker
            listToAddAll.AddRange(_dbContext.Orders.Include(o => o.Requests)
                .Include(o => o.Language).Where(o => o.Status == OrderStatus.CancelledByBroker && o.CreatedBy == User.GetUserId())
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CancelledAt.Value.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderCancelled, ButtonAction = "View", ButtonController = "Order" }).ToList());

            var count = listToAddAll.Any() ? listToAddAll.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av myndighet ({count} st)" : "Kräver handling av myndighet",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som kräver handling av myndigheten",
                StartListObjects = listToAddAll,
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
                Header = count > 0 ? $"Skickade bokningar ({count} st)" : "Skickade bokningar",
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
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är tillsatta",
                StartListObjects = approvedOrders
            };

            // Awaiting requisition
            var awaitRequisition = _dbContext.Orders.Include(o => o.Requests)
            .Include(o => o.Language).Where(o => o.Status == OrderStatus.ResponseAccepted &&
            o.CreatedBy == User.GetUserId() && o.EndAt < _clock.SwedenNow && !(o.Requests.Any(r => r.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.AutomaticApprovalFromCancelledOrder || req.Status == RequisitionStatus.Created)))).Select
            (o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt}, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.RequisitionAwaited }).ToList();

            count = awaitRequisition.Any() ? awaitRequisition.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Inväntar rekvisition ({count} st)" : "Inväntar rekvisition",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som inväntar rekvisition",
                StartListObjects = awaitRequisition
            };
        }

        private async Task<IEnumerable<StartViewModel.ConfirmationMessage>> GetConfirmationMessages()
        {
            if ((await _authorizationService.AuthorizeAsync(User, Policies.Interpreter)).Succeeded)
            {
                return await _dbContext.InterpreterBrokers
                    .Where(ib => ib.InterpreterId == User.GetInterpreterId() && !ib.AcceptedByInterpreter)
                    .Select(ib => new StartViewModel.ConfirmationMessage
                    {
                        Header = $"Ny förmedling {ib.Broker.Name}",
                        Message = $"Förmedling {ib.Broker.Name} har lagt till dig som tolk för att kunna skicka uppdrag till dig.",
                        Controller = "Interpreter",
                        Action = "AcceptBroker",
                        Id = ib.BrokerId
                    }).ToListAsync();
            }

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
