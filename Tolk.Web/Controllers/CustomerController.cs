using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
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
        private readonly ISwedishClock _clock;
        private readonly CustomerOrganisationService _customerService;

        private int CentralAdministratorRoleId => _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
        private int CentralOrderHandlerRoleId => _roleManager.Roles.Single(r => r.Name == Roles.CentralOrderHandler).Id;

        public CustomerController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            RoleManager<IdentityRole<int>> roleManager,
            INotificationService notificationService,
            CacheService cacheService,
            ISwedishClock clock,
            CustomerOrganisationService customerService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _roleManager = roleManager;
            _notificationService = notificationService;
            _cacheService = cacheService;
            _clock = clock;
            _customerService = customerService;
        }

        public ActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        public ActionResult List(CustomerFilterModel model)
        {
            model ??= new CustomerFilterModel();

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
            });
        }

        public async Task<ActionResult> View(int id, string message)
        {
            var customer = await _dbContext.CustomerOrganisations.GetCustomerById(id);
            customer.CustomerSettings = await _dbContext.CustomerSettings.GetCustomerSettingsForCustomer(id).ToListAsync();
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
            customer.CustomerSettings = await _dbContext.CustomerSettings.GetCustomerSettingsForCustomer(id).ToListAsync();
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
            var customer = _dbContext.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == model.CustomerId);
            customer.CustomerSettings = await _dbContext.CustomerSettings.GetCustomerSettingsForCustomer(customer.CustomerOrganisationId).ToListAsync();
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded)
            {
                if (ModelState.IsValid && ValidateOrderResponseDate(model))
                {
                    var customerSettings = model.CustomerSettings.Select(cs => new CustomerSetting { CustomerSettingType = cs.CustomerSettingType, Value = cs.Value });
                    customer.UpdateCustomerSettingsAndHistory(_clock.SwedenNow, User.GetUserId(), customerSettings);                  
                    customer.CustomerOrderAgreementSettings = await _customerService.UpdateOrderAgreementSettings(customer, model.ShowUseOrderAgreementsFromDate ? model.UseOrderAgreementsFromDate : null, User.GetUserId());
                    model.UpdateCustomer(customer);
                    await _dbContext.SaveChangesAsync();

                    await _cacheService.Flush(CacheKeys.CustomerOrderAgreementSettings);
                    await _cacheService.Flush(CacheKeys.CustomerSettings);
                    await _cacheService.Flush(CacheKeys.OrganisationSettings);
                    return RedirectToAction(nameof(View), new { Id = model.CustomerId, Message = "Myndighet har uppdaterats" });
                }
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public ActionResult Create()
        {
            return View(new CustomerModel { IsCreating = true, CustomerSettings = EmptyCustomerSettings });
        }

        private static List<CustomerSettingModel> EmptyCustomerSettings =>
            EnumHelper.GetAllDescriptions<CustomerSettingType>().OrderBy(e => e.Description)
            .Select(e => new CustomerSettingModel() { CustomerSettingType = e.Value, Value = false }).ToList();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<ActionResult> Create(CustomerModel model)
        {            
            if (ModelState.IsValid && ValidateCustomer(model) && ValidateOrderResponseDate(model))
            {
                CustomerOrganisation customer = new CustomerOrganisation();
                model.UpdateCustomer(customer, true);
                _dbContext.Add(customer);
                customer.CustomerOrderAgreementSettings = await _customerService.CreateInitialCustomerOrderAgreementSettings(customer.UseOrderAgreementsFromDate);
                await _dbContext.SaveChangesAsync();
                if (customer.UseOrderAgreementsFromDate.HasValue)
                {
                    await _cacheService.Flush(CacheKeys.CustomerOrderAgreementSettings);
                }
                await _cacheService.Flush(CacheKeys.CustomerSettings);
                customer = await _dbContext.CustomerOrganisations.GetCustomerById(customer.CustomerOrganisationId);
                _notificationService.CustomerCreated(customer);
                return RedirectToAction(nameof(View), new { Id = customer.CustomerOrganisationId, Message = "Myndighet har skapats" });
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListUsers(IDataTablesRequest request)
        {
            //Get filters
            CustomerUserFilterModel filters = await GetUserFilters();

            //Get the full table
            var data = _dbContext.Users.Where(u => u.CustomerOrganisationId == filters.UserFilterModelCustomerId);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListUnits(IDataTablesRequest request)
        {
            //Get filters
            AdminUnitFilterModel filters = await GetUnitFilters();

            //Get the full table
            var data = _dbContext.CustomerUnits.Where(u => u.CustomerOrganisationId == filters.AdminUnitFilterModelCustomerId);

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

        public JsonResult GetCustomerSpecificColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<CustomerSpecificPropertyListItemModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListSpecificProperties(IDataTablesRequest request)
        {
            //Get filters
            var filters = await GetCustomerSpecificPropertyFilters();

            //Get the Full Model           
            var data = _cacheService.AllCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == filters.CustomerSpecificPropertyFilterModelCustomerId).AsQueryable();

            return AjaxDataTableHelper.GetData(request, data.Count(), data, d => d.Select(csp => new CustomerSpecificPropertyListItemModel
            {
                CustomerOrganisationId = csp.CustomerOrganisationId,
                DisplayName = csp.DisplayName,
                PropertyTypeName = csp.PropertyToReplace.GetDescription(),
                PropertyType = csp.PropertyToReplace,
                RegexPattern = csp.RegexPattern,
                Enabled = csp.Enabled,
            }));
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

        private async Task<CustomerSpecificPropertyFilterModel> GetCustomerSpecificPropertyFilters()
        {
            var filters = new CustomerSpecificPropertyFilterModel();
            await TryUpdateModelAsync(filters);
            return filters;
        }
        private bool ValidateCustomer(CustomerModel model)
        {
            bool valid = true;
            //Test prefix
            if (_dbContext.CustomerOrganisations.Any(c =>
                 c.CustomerOrganisationId != model.CustomerId &&
                 c.OrganisationPrefix != null && c.OrganisationPrefix.ToLower() == model.OrganisationPrefix.ToLower()))
            {
                ModelState.AddModelError(nameof(model.OrganisationPrefix), $"Denna Namnprefix används redan i tjänsten.");
                valid = false;
            }
            if (model.UseOrderResponsesFromDate < model.UseOrderAgreementsFromDate)
            {
                ModelState.AddModelError(nameof(model.UseOrderResponsesFromDate), $"Order Response kan inte skickas tidigare än Order Agreement.");
                valid = false;
            }
            return valid;
        }
        private bool ValidateOrderResponseDate(CustomerModel model)
        {
            bool valid = true;
            if (model.UseOrderResponsesFromDate < model.UseOrderAgreementsFromDate)
            {
                ModelState.AddModelError(nameof(model.UseOrderResponsesFromDate), $"Order Response kan inte skickas tidigare än Order Agreement.");
                valid = false;
            }
            return valid;
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> EditOrderAgreementSettings(int customerOrganisationId, string message = null, string errorMessage = null)
        {
            var customer = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == customerOrganisationId);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded)
            {
                var customerOrderAgreementSettings = _cacheService.CustomerOrderAgreementSettings.Where(coas => coas.CustomerOrganisationId == customerOrganisationId).ToList();
                var settingsModel = new CustomerOrderAgreementSettingsViewModel()
                {
                    CustomerOrganisationId = customerOrderAgreementSettings.First().CustomerOrganisationId,
                    CustomerOrganisation = customerOrderAgreementSettings.First().CustomerName,
                    Message = message,
                    ErrorMessage = errorMessage
                };
                settingsModel.CustomerOrderAgreementBrokerSettings =
                    customerOrderAgreementSettings
                        .Select(coas => new CustomerOrderAgreementBrokerListModel
                        {
                            BrokerId = coas.BrokerId,
                            BrokerName = coas.BrokerName,
                            Disabled = coas.Disabled
                        }).ToList();

                return View(settingsModel);
            }
            return Forbid();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> ChangeOrderAgreementSettings(int customerOrganisationId, int brokerId)
        {            
            var customer = _dbContext.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == customerOrganisationId);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded)
            {
                await _customerService.ToggleSpecificOrderAgreementSettings(customerOrganisationId, brokerId, User.GetUserId());
                await _cacheService.Flush(CacheKeys.CustomerOrderAgreementSettings);
                return RedirectToAction(nameof(EditOrderAgreementSettings), "Customer", new
                {
                    customerOrganisationId,
                    message = "Inställningarna har uppdaterats"
                });
            }

            return Forbid();
        }
    }
}
