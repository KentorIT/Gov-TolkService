using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class HomeController : Controller
    {
        private TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public HomeController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
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
            var customerId = User.Claims.SingleOrDefault(c => c.Type == TolkClaimTypes.CustomerOrganisationId)?.Value;
            if (customerId != null)
            {
                yield return new StartPageBox
                {
                    Count = _dbContext.Orders.Where(o => o.Status == OrderStatus.RequestResponded && o.CreatedBy == _userManager.GetUserId(User)).Count(),
                    Header = "Tillsatt tolk",
                    Controller = "Order",
                    Action = "List"
                };
                yield return new StartPageBox
                {
                    Count = _dbContext.Orders.Where(o => o.Status == OrderStatus.Delivered && o.CreatedBy == _userManager.GetUserId(User)).Count(),
                    Header = "Rekvirerade",
                    Controller = "Order",
                    Action = "List"
                };
            }
            var brokerId = User.Claims.SingleOrDefault(c => c.Type == TolkClaimTypes.BrokerId)?.Value;
            if (brokerId != null)
            {
                yield return new StartPageBox
                {
                    Count = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) && 
                        r.Ranking.BrokerId == int.Parse(brokerId)).Count(),
                    Header = "Inkomna förfrågningar",
                    Controller = "Request",
                    Action = "List"
                };
                yield return new StartPageBox
                {
                    Count = 0,
                    Header = "Ändrade förfrågningar",
                    Controller = "Request",
                    Action = "List"
                };
            }
            var interpreterId = User.Claims.SingleOrDefault(c => c.Type == TolkClaimTypes.InterpreterId)?.Value;
            if (interpreterId != null)
            {
                yield return new StartPageBox
                {
                    //TODO: Here we need to check the order too!
                    Count = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Approved) && r.InterpreterId == int.Parse(interpreterId)).Count(),
                    Header = "Tillsatta uppdrag",
                    Controller = "Assignment",
                    Action = "List"
                };
                yield return new StartPageBox
                {
                    Count = 0,
                    Header = "Att avrapportera",
                    Controller = "Assignment",
                    Action = "List"
                };
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
    }
}
