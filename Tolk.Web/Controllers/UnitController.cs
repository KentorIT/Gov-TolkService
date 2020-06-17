using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policies.SystemCentralLocalAdmin)]
    public class UnitController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;
        private readonly UserService _userService;

        public UnitController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            UserService userService
        )
        {
            _dbContext = dbContext;
            _clock = clock;
            _authorizationService = authorizationService;
            _userService = userService;
        }

        [Authorize(Policies.CentralLocalAdminCustomer)]
        public ActionResult List(UnitFilterModel model)
        {
            if (model == null)
            {
                model = new UnitFilterModel();
            }

            IEnumerable<int> localAdminUnits = User.TryGetLocalAdminCustomerUnits() ?? new List<int>();
            var units = _dbContext.CustomerUnits
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
                }),
                AllowCreation = User.IsInRole(Roles.CentralAdministrator)
            });
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> Create(CustomerUnitModel model)
        {
            if (ModelState.IsValid)
            {
                if (!IsUniqueName(model.Name))
                {
                    ModelState.AddModelError(nameof(model.Name), $"Namnet används redan för en annan enhet för denna myndighet");
                }
                else if (!_userService.IsUniqueEmail(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), $"E-postadressen används redan i tjänsten");
                }
                else
                {
                    int? customerId = User.TryGetCustomerOrganisationId();
                    var unit = new CustomerUnit();
                    unit.Create(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), User.GetCustomerOrganisationId(), model.Name, model.Email, model.LocalAdministrator);
                    await _userService.LogCustomerUnitUserUpdateAsync(model.LocalAdministrator, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.AddAsync(unit);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(View), new { id = unit.CustomerUnitId });
                }
            }
            return View(model);
        }

        [Authorize(Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> View(int id)
        {
            var unit = await GetUnitToHandle(id);
            if (unit != null)
            {
                if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.View)).Succeeded)
                {
                    return View(CustomerUnitModel.GetModelFromCustomerUnit(unit));
                }
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public async Task<ActionResult> AdminView(int id)
        {
            var unit = await GetUnitToHandle(id);
            if (unit != null)
            {
                if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.View)).Succeeded)
                {
                    var model = CustomerUnitModel.GetModelFromCustomerUnit(unit);
                    model.UnitUsers = await GetUnitUsersListItems(await GetUnitUsersIds(id), id);
                    return View(model);
                }
            }
            return Forbid();
        }

        [Authorize(Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> Edit(int id)
        {
            var unit = await GetUnitToHandle(id);
            if (unit != null)
            {
                if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.Edit)).Succeeded)
                {
                    return View(CustomerUnitModel.GetModelFromCustomerUnit(unit, User.IsInRole(Roles.CentralAdministrator)));
                }
            }
            return Forbid();
        }

        private async Task<CustomerUnit> GetUnitToHandle(int id)
        {
            return await _dbContext.CustomerUnits.GetCustomerUnitById(id);
        }

        [Authorize(Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> Users(int id, string errorMessage = null, string message = null)
        {
            var unit = await _dbContext.CustomerUnits.GetCustomerUnitById(id);
            var users = await GetUnitUsersListItems(await GetUnitUsersIds(id), id);

            if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.Edit)).Succeeded)
            {
                var model = new CustomerUnitModel
                {
                    Message = message,
                    ErrorMessage = errorMessage,
                    CustomerUnitId = id,
                    Name = unit.Name,
                    UnitUsers = users,
                    UserPageMode = new UserPageMode
                    {
                        BackController = "Unit",
                        BackAction = nameof(Users),
                        BackId = id.ToSwedishString()
                    }
                };
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CustomerUnitModel model)
        {
            if (ModelState.IsValid)
            {
                if (!IsUniqueName(model.Name, model.CustomerUnitId))
                {
                    ModelState.AddModelError(nameof(model.Name), $"Namnet används redan för en annan enhet för denna myndighet");
                }
                else if (!_userService.IsUniqueEmail(model.Email, customerUnitId: model.CustomerUnitId))
                {
                    ModelState.AddModelError(nameof(model.Email), $"E-postadressen används redan i tjänsten");
                }
                else
                {
                    var unit = _dbContext.CustomerUnits
                    .SingleOrDefault(cu => cu.CustomerUnitId == model.CustomerUnitId);
                    if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.Edit)).Succeeded)
                    {
                        unit.Update(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.Name, model.Email, model.IsActive);
                        await _dbContext.SaveChangesAsync();
                        return RedirectToAction(nameof(View), new { id = model.CustomerUnitId });
                    }
                    return Forbid();
                }
                model.IsCentralAdministrator = User.IsInRole(Roles.CentralAdministrator);
            }
            return View(model);
        }

        private bool IsUniqueName(string name, int? customerUnitId = null)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Name.ToSwedishUpper() == name.ToSwedishUpper() && u.CustomerUnitId != customerUnitId);
        }

        private async Task<IEnumerable<int>> GetUnitUsersIds(int customerUnitId)
        {
            return await _dbContext.CustomerUnitUsers.GetUserIdsForCustomerUnitsWithCustomerUnitId(customerUnitId);
        }

        private async Task<IEnumerable<DynamicUserListItemModel>> GetUnitUsersListItems(IEnumerable<int> unitUsersIds, int customerUnitId)
        {
            var customerUnitUsers = await _dbContext.CustomerUnitUsers.GetCustomerUnitsWithCustomerUnitForAllUsers(unitUsersIds, customerUnitId).ToArrayAsync();
            return _dbContext.Users.GetUsersByUserIds(unitUsersIds).Select(u => new DynamicUserListItemModel
            {
                Id = u.Id,
                CombinedId = u.Id + "_" + customerUnitId,
                FirstName = u.NameFirst,
                LastName = u.NameFamily,
                Email = u.Email,
                IsActive = u.IsActive,
                IsLocalAdmin = customerUnitUsers.Any(cu => cu.UserId == u.Id && cu.IsLocalAdmin) ? "Ja" : "Nej"
            });
        }
    }
}
