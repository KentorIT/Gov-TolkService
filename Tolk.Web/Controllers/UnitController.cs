using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class UnitController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserService _userService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;
        private readonly HashService _hashService;

        public UnitController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            ILogger<UserController> logger,
            RoleManager<IdentityRole<int>> roleManager,
            UserService userService,
            IAuthorizationService authorizationService,
            INotificationService notificationService,
            HashService hashService
        )
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
            _roleManager = roleManager;
            _userService = userService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
            _hashService = hashService;
        }

        [Authorize(Policies.CentralOrLocalAdmin)]
        public ActionResult List(UnitFilterModel model)
        {
            if (model == null)
            {
                model = new UnitFilterModel();
            }
            IEnumerable<int> localAdminUnits = User.TryGetLocalAdminCustomerUnits()?? new List<int>();
            var units = _dbContext.CustomerUnits
               .Include(s => s.CreatedByUser)
               .Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId() 
               && (localAdminUnits.Contains(cu.CustomerUnitId) || User.IsInRole(Roles.CentralAdministrator)));

            units = model.Apply(units);

            return View(new UnitListModel
            {
                Items =
                units.
                Select(cu => new UnitListItemModel
                {
                    Name = cu.Name,
                    CreatedBy = cu.CreatedByUser.FullName,
                    CreatedAt = cu.CreatedAt,
                    IsActive = cu.IsActive,
                    CustomerUnitId = cu.CustomerUnitId,
                    Email = cu.Email
                })
            });
        }
    }
}
