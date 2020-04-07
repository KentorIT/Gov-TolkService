using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

    [Authorize(Roles = Roles.AppOrSysAdmin)]
    public class CustomerController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly INotificationService _notificationService;
        private readonly CacheService _cacheService;

        private int CentralAdministratorRoleId => _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
        private int CentralOrderHandlerRoleId => _roleManager.Roles.Single(r => r.Name == Roles.CentralOrderHandler).Id;

        public CustomerController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            RoleManager<IdentityRole<int>> roleManager,
            INotificationService notificationService,
            CacheService cacheService
        )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _roleManager = roleManager;
            _notificationService = notificationService;
            _cacheService = cacheService;
        }

        public ActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        public ActionResult List(CustomerFilterModel model)
        {
            if (model == null)
            {
                model = new CustomerFilterModel();
            }

            return View(new CustomerListModel
            {
                FilterModel = model,
                Items = model.Apply(_dbContext.CustomerOrganisations)
                    .Select(c =>
                    new CustomerListItemModel
                    {
                        CustomerId = c.CustomerOrganisationId,
                        Name = c.Name,
                        PriceListType = c.PriceListType,
                        ParentName = c.ParentCustomerOrganisation.Name,
                        OrganisationNumber = c.OrganisationNumber
                    }),
                AllowCreate = User.IsInRole(Roles.ApplicationAdministrator)
            });;
        }

        public async Task<ActionResult> View(int id, string message)
        {
            var customer = await _dbContext.CustomerOrganisations
                .Include(c => c.ParentCustomerOrganisation)
                .SingleAsync(c => c.CustomerOrganisationId == id);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.View)).Succeeded)
            {
                return View(CustomerModel.GetModelFromCustomer(customer, message, (await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded));
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<ActionResult> Edit(int id)
        {
            var customer = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == id);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded)
            {
                return View(CustomerModel.GetModelFromCustomer(customer));
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<ActionResult> Edit(CustomerModel model)
        {
            var customer = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == model.CustomerId);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded)
            {
                if (ModelState.IsValid && ValidateCustomer(model))
                {
                    model.UpdateCustomer(customer);
                    await _dbContext.SaveChangesAsync();
                    await _cacheService.Flush(CacheKeys.Customers);
                    return RedirectToAction(nameof(View), new { Id = model.CustomerId, Message = "Myndighet har uppdaterats" });
                }
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public ActionResult Create()
        {
            return View(new CustomerModel { IsCreating = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<ActionResult> Create(CustomerModel model)
        {
            if (ModelState.IsValid && ValidateCustomer(model))
            {
                CustomerOrganisation customer = new CustomerOrganisation();
                model.UpdateCustomer(customer);
                customer.PriceListType = model.PriceListType.Value;
                customer.OrganisationPrefix = model.OrganisationPrefix;
                customer.TravelCostAgreementType = model.TravelCostAgreementType.Value;
                _dbContext.Add(customer);
                await _cacheService.Flush(CacheKeys.Customers);
                await _dbContext.SaveChangesAsync();
                customer = await _dbContext.CustomerOrganisations
                    .Include(c => c.ParentCustomerOrganisation)
                    .SingleAsync(c => c.CustomerOrganisationId == customer.CustomerOrganisationId);
                _notificationService.CustomerCreated(customer);
                return RedirectToAction(nameof(View), new { Id = customer.CustomerOrganisationId, Message = "Myndighet har skapats" });
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ListUsers(IDataTablesRequest request)
        {
            //Get filters
            CustomerUserFilterModel filters = await GetUserFilters();

            //Get the full table
            var data = _dbContext.Users.Where(u => u.CustomerOrganisationId == filters.Id);

            //Filter and return data tables data
            return AjaxDataTableHelper.GetData(request, data.Count(), DynamicUserListItemModel.Filter(filters, data), d => d.Select(u => new DynamicUserListItemModel
            {
                Id = u.Id,
                FirstName = u.NameFirst,
                LastName = u.NameFamily,
                Email = u.Email,
                IsActive = u.IsActive
            }));
        }

        [HttpPost]
        public async Task<IActionResult> ListUnits(IDataTablesRequest request)
        {
            //Get filters
            AdminUnitFilterModel filters = await GetUnitFilters();

            //Get the full table
            var data = _dbContext.CustomerUnits.Where(u => u.CustomerOrganisationId == filters.Id);

            //Filter and return data tables data
            return AjaxDataTableHelper.GetData(request, data.Count(), AdminUnitListItemModel.Filter(filters, data), d => d.Select(u => new AdminUnitListItemModel
            {
                CustomerUnitId = u.CustomerUnitId,
                Name = u.Name,
                Email = u.Email,
                IsActive = u.IsActive
            }));
        }
        public JsonResult UserColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<DynamicUserListItemModel>());
        }

        public JsonResult UnitColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<AdminUnitListItemModel>());
        }

        private async Task<CustomerUserFilterModel> GetUserFilters()
        {
            var filters = new CustomerUserFilterModel();
            await TryUpdateModelAsync(filters);
            filters.CentralAdministratorRoleId = CentralAdministratorRoleId;
            filters.CentralOrderHandlerRoleId = CentralOrderHandlerRoleId;
            return filters;
        }

        private async Task<AdminUnitFilterModel> GetUnitFilters()
        {
            var filters = new AdminUnitFilterModel();
            await TryUpdateModelAsync(filters);
            return filters;
        }

        private bool ValidateCustomer(CustomerModel model)
        {
            bool valid = true;
            //Test prefix
            if (_dbContext.CustomerOrganisations.Any(c =>
                 c.CustomerOrganisationId != model.CustomerId &&
                 c.OrganisationPrefix != null && c.OrganisationPrefix.Equals(model.OrganisationPrefix, StringComparison.InvariantCultureIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.OrganisationPrefix), $"Denna Namnprefix används redan i tjänsten.");
                valid = false;
            }
            return valid;
        }
    }
}
