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

        public UnitController(
            TolkDbContext dbContext,
            ISwedishClock clock
        )
        {
            _dbContext = dbContext;
            _clock = clock;
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
                    return RedirectToAction(nameof(List), new UserFilterModel { });
                }
            }
            return View(model);
        }

        private bool IsUniqueEmail(string email)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Email.ToUpper() == email.ToUpper());
        }

        private bool IsUniqueName(string name)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Name.ToUpper() == name.ToUpper());
        }
    }
}
