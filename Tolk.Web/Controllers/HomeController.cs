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

namespace Tolk.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly ISwedishClock _clock;

        public HomeController(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _clock = clock;
        }

        public IActionResult Index(string message)
        {
            if (!_dbContext.IsUserStoreInitialized)
            {
                return RedirectToAction("CreateInitialUser", "Account");
            }
            var startBoxes = GetStartPageBoxes();
            return View(new StartViewModel { Message = message, Boxes = startBoxes });
        }

        private IEnumerable<StartPageBox> GetStartPageBoxes()
        {
            var customerId = User.TryGetCustomerOrganisationId();
            if (customerId.HasValue)
            {
                yield return new StartPageBox
                {
                    Count = _dbContext.Orders.Where(o => o.Status == OrderStatus.RequestResponded && o.CreatedBy == User.GetUserId()).Count(),
                    Header = "Tillsatt tolk",
                    Controller = "Order",
                    Action = "List"
                };
                yield return new StartPageBox
                {
                    Count = _dbContext.Requisitions.Where(r => r.Status == RequisitionStatus.Created &&
                        r.Request.Order.Status == OrderStatus.Delivered && 
                        r.Request.Order.CreatedBy == User.GetUserId()).Count(),
                    Header = "Rekvisitioner att kontrollera",
                    Controller = "Requisition",
                    Action = "List"
                };
            }
            var brokerId = User.TryGetBrokerId();
            if (brokerId.HasValue)
            {
                yield return new StartPageBox
                {
                    Count = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) && 
                        r.Ranking.BrokerId == brokerId.Value).Count(),
                    Header = "Inkomna förfrågningar",
                    Controller = "Request",
                    Action = "List"
                };
                yield return new StartPageBox
                {
                    Count = _dbContext.Requests.Where(r => r.Status == RequestStatus.Approved &&
                        r.Order.StartDateTime < _clock.SwedenNow &&
                        !r.Requisitions.Any() &&
                        r.Ranking.BrokerId == brokerId).Count(),
                    Header = "Tolktillfällen att avrapportera",
                    Controller = "Assignment",
                    Action = "List"
                };
                int count = _dbContext.Requisitions.Where(r => !r.ReplacedByRequisitionId.HasValue && 
                        r.Status == RequisitionStatus.DeniedByCustomer &&
                       !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) &&
                       r.Request.Ranking.BrokerId == brokerId).Count();
                if (count > 0)
                {
                    yield return new StartPageBox
                    {
                        Count = count,
                        Header = "Nekade rekvisitioner",
                        Controller = "Requisition",
                        Action = "List"
                    };
                }
            }
            var interpreterId = User.TryGetInterpreterId();
            if (interpreterId.HasValue)
            {
                yield return new StartPageBox
                {
                    //TODO: Here we need to check the order too!
                    Count = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Approved) &&
                        r.Order.StartDateTime > _clock.SwedenNow && 
                        r.InterpreterId == interpreterId.Value).Count(),
                    Header = "Kommande uppdrag",
                    Controller = "Assignment",
                    Action = "List"
                };
                yield return new StartPageBox
                {
                    Count = _dbContext.Requests.Where(r => r.Status == RequestStatus.Approved && 
                        r.Order.StartDateTime < _clock.SwedenNow &&    
                        !!r.Requisitions.Any() && 
                        r.InterpreterId == interpreterId.Value).Count(),
                    Header = "Att avrapportera",
                    Controller = "Assignment",
                    Action = "List"
                };
                int count = _dbContext.Requisitions.Where(r => !r.ReplacedByRequisitionId.HasValue &&
                    r.Status == RequisitionStatus.DeniedByCustomer &&
                       !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) &&
                      r.Request.InterpreterId == interpreterId.Value).Count();
                if (count > 0)
                {
                    yield return new StartPageBox
                    {
                        Count = count,
                        Header = "Nekade rekvisitioner",
                        Controller = "Requisition",
                        Action = "List"
                    };
                }
            }
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

            switch(action)
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
