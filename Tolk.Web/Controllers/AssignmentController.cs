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
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Interpreter)]
    public class AssignmentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IAuthorizationService _authorizationService;

        public AssignmentController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public IActionResult List()
        {
            return View(_dbContext.Requests.Include(r => r.Order)
                .Where(r => (r.Status == RequestStatus.Approved) &&
                    r.InterpreterId == User.GetInterpreterId())
                    .Select(r => new RequestListItemModel
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
            // Remember to add authorization once this method loads data.
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Edit(RequestModel model)
        {
            // Remember to add authorization once this method saves data.
            return View("Edit", model);
        }
    }
}
