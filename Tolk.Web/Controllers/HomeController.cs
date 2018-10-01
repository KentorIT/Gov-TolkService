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
                Message = message,
                Boxes = (await GetStartPageBoxes())
                // Uncomment this block to get a bunch of extra boxes for layout testing.
                //.Union(
                //    Enumerable.Range(1, 10)
                //    .Select(i => new StartViewModel.StartPageBox
                //    {
                //        Header = $"H{i}",
                //        Count = i,
                //        Action = "Index",
                //        Controller = "Home"
                //    })),
                , // Yes, a single , on a line by itself is intentional.
                ConfirmationMessages = await GetConfirmationMessages()
            });
        }

        private async Task<IEnumerable<StartViewModel.StartPageBox>> GetStartPageBoxes()
        {
            var result = Enumerable.Empty<StartViewModel.StartPageBox>();

            if ((await _authorizationService.AuthorizeAsync(User, Policies.Customer)).Succeeded)
            {
                result = result.Union(GetCustomerStartPageBoxes());
            }

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

        private IEnumerable<StartViewModel.StartPageBox> GetCustomerStartPageBoxes()
        {
            yield return new StartViewModel.StartPageBox
            {
                Count = _dbContext.Orders.Where(o => (o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.RequestRespondedNewInterpreter) && o.CreatedBy == User.GetUserId()).Count(),
                Header = "Tillsatt tolk",
                Controller = "Order",
                Action = "List",
                Filters = new Dictionary<string, string> { { "Status", OrderStatus.ToBeProcessedByCustomer.ToString() } }
            };

            yield return new StartViewModel.StartPageBox
            {
                Count = _dbContext.Requisitions.Where(r => r.Status == RequisitionStatus.Created &&
                    r.Request.Order.Status == OrderStatus.Delivered &&
                    r.Request.Order.CreatedBy == User.GetUserId()).Count(),
                Header = "Rekvisitioner att kontrollera",
                Controller = "Requisition",
                Action = "List",
                Filters = new Dictionary<string, string> {
                        { "Status", RequisitionStatus.Created.ToString() },
                        { "FilterByContact", "false"}
                    }
            };

            int count = _dbContext.Requisitions.Where(r => r.Status == RequisitionStatus.Created &&
                    r.Request.Order.Status == OrderStatus.Delivered &&
                    r.Request.Order.ContactPersonId == User.GetUserId()).Count();

            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Rekvisitioner att kontrollera som kontakt",
                    Controller = "Requisition",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", RequisitionStatus.Created.ToString() },
                        { "FilterByContact", "true"}
                    }
                };
            }

            count = _dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Disputed &&
                    c.CreatedBy == User.GetUserId()).Count();

            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Bestridda reklamationer",
                    Controller = "Complaint",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", ComplaintStatus.Disputed.ToString() }
                    }
                };
            }

            count = _dbContext.Orders.Where(o => o.Status == OrderStatus.CancelledByBroker &&
                o.CreatedBy == User.GetUserId()).Count();

            if (count > 0)
            {
                yield return new StartViewModel.StartPageBox
                {
                    Count = count,
                    Header = "Avbokade avrop från förmedling",
                    Controller = "Order",
                    Action = "List",
                    Filters = new Dictionary<string, string> { { "Status", OrderStatus.CancelledByBroker.ToString() } }
                };
            }
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
                    r.Ranking.BrokerId == brokerId &&
                    !r.Order.ReplacingOrderId.HasValue).Count(),
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
                    Header = "Nekade rekvisitioner",
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
                    r.InterpreterId == interpreterId &&
                    !r.Order.ReplacingOrderId.HasValue).Count(),
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
                    Header = "Nekade rekvisitioner",
                    Controller = "Requisition",
                    Action = "List",
                    Filters = new Dictionary<string, string> {
                        { "Status", RequisitionStatus.DeniedByCustomer.ToString() }
                    }

                };
            }
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
