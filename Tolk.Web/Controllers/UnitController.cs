using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policies.CentralOrLocalAdmin)]
    public class UnitController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;


        public UnitController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            IAuthorizationService authorizationService
        )
        {
            _dbContext = dbContext;
            _clock = clock;
            _authorizationService = authorizationService;
        }

        public ActionResult List(UnitFilterModel model)
        {
            if (model == null)
            {
                model = new UnitFilterModel();
            }

            IEnumerable<int> localAdminUnits = User.TryGetLocalAdminCustomerUnits() ?? new List<int>();
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
                    ModelState.AddModelError(nameof(model.Name), $"Namnet används redan för en annan enhet för denna myndighet.");
                }
                else if (!IsUniqueEmail(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), $"E-postadressen används redan för en annan enhet för denna myndighet.");
                }
                else
                {
                    int? customerId = User.TryGetCustomerOrganisationId();
                    var unit = new CustomerUnit();
                    unit.Create(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), User.GetCustomerOrganisationId(), model.Name, model.Email, model.LocalAdministrator);
                    await _dbContext.AddAsync(unit);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(List));
                }
            }
            return View(model);
        }

        public async Task<ActionResult> View(int id)
        {
            var unit = _dbContext.CustomerUnits
               .Include(s => s.CreatedByUser)
               .Include(s => s.InactivatedByUser)
               .SingleOrDefault(cu => cu.CustomerUnitId == id);
            if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.View)).Succeeded)
            {
                var model = new CustomerUnitModel
                {
                    Id = id,
                    Name = unit.Name,
                    Email = unit.Email,
                    CreatedAt = unit.CreatedAt,
                    CreatedBy = unit.CreatedByUser.FullName,
                    IsActive = unit.IsActive,
                    InactivatedAt = unit.InactivatedAt,
                    InactivatedBy = unit.InactivatedByUser?.FullName ?? string.Empty
                };
                return View(model);
            }
            return Forbid();
        }

        public async Task<ActionResult> Edit(int id)
        {
            var unit = _dbContext.CustomerUnits
               .Include(s => s.CreatedByUser)
               .Include(s => s.InactivatedByUser)
               .SingleOrDefault(cu => cu.CustomerUnitId == id);
            if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.Edit)).Succeeded)
            {
                var model = new CustomerUnitModel
                {
                    Id = id,
                    Name = unit.Name,
                    Email = unit.Email,
                    IsActive = unit.IsActive,
                    IsCentralAdministrator = User.IsInRole(Roles.CentralAdministrator)
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
                if (!IsUniqueName(model.Name, model.Id))
                {
                    ModelState.AddModelError(nameof(model.Name), $"Namnet används redan för en annan enhet för denna myndighet.");
                }
                else if (!IsUniqueEmail(model.Email, model.Id))
                {
                    ModelState.AddModelError(nameof(model.Email), $"E-postadressen används redan för en annan enhet för denna myndighet.");
                }
                else
                {
                    var unit = _dbContext.CustomerUnits
                    .SingleOrDefault(cu => cu.CustomerUnitId == model.Id);
                    if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.Edit)).Succeeded)
                    {
                        unit.Update(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.Name, model.Email, model.IsActive);
                        await _dbContext.SaveChangesAsync();
                        return RedirectToAction(nameof(View), new { id = model.Id });
                    }
                    return Forbid();
                }
                model.IsCentralAdministrator = User.IsInRole(Roles.CentralAdministrator);
            }
            return View(model);
        }

        private bool IsUniqueEmail(string email, int? customerUnitId = null)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Email.ToUpper() == email.ToUpper() && u.CustomerUnitId != customerUnitId);
        }

        private bool IsUniqueName(string name, int? customerUnitId = null)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Name.ToUpper() == name.ToUpper() && u.CustomerUnitId != customerUnitId);
        }
    }
}
