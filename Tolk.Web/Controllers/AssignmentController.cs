using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Interpreter)]
    public class AssignmentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public AssignmentController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        protected int CurrentInterpreterId
        {
            get
            {
                return int.Parse(User.Claims.Single(c => c.Type == TolkClaimTypes.InterpreterId).Value);
            }
        }

        public IActionResult List()
        {
            return View(_dbContext.Requests.Include(r => r.Order)
                .Where(r => (r.Status == RequestStatus.Approved) &&
                    r.InterpreterId == CurrentInterpreterId).Select(r => new RequestListItemModel
                    {
                        RequestId = r.RequestId,
                        Language = r.Order.Language.Name,
                        OrderNumber = r.Order.OrderNumber.ToString(),
                        CustomerName = r.Order.CustomerOrganisation.Name,
                        RegionName = r.Order.Region.Name,
                        Start = r.Order.StartDateTime,
                        End = r.Order.EndDateTime,
                        Status = r.Status
                    }));
        }

        public IActionResult Edit(int id)
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Edit(RequestModel model)
        {
            return View("Edit", model);
        }
    }
}
