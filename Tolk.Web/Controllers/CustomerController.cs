using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Validation;
using Tolk.Web.Authorization;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.SystemAdministrator)]
    public class CustomerController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly IAuthorizationService _authorizationService;


        public CustomerController(
            TolkDbContext dbContext,
            ILogger<ContractController> logger,
            IAuthorizationService authorizationService
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _authorizationService = authorizationService;
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
                        ParentName = c.ParentCustomerOrganisation.Name
                    })
            });
        }

        public async Task<ActionResult> View(int id, string message)
        {
            var customer = _dbContext.CustomerOrganisations
                .Include(c => c.ParentCustomerOrganisation)
                .Single(c => c.CustomerOrganisationId == id);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.View)).Succeeded)
            {
                return View(CustomerModel.GetModelFromCustomer(customer, message));
            }
            return Forbid();
        }

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
        public async Task<ActionResult> Edit(CustomerModel model)
        {
            var customer = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == model.CustomerId);
            if ((await _authorizationService.AuthorizeAsync(User, customer, Policies.Edit)).Succeeded)
            {
                if (ModelState.IsValid && ValidateCustomer(model))
                {
                    model.UpdateCustomer(customer);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(View), new { Id = model.CustomerId, Message = "Myndighet har uppdaterats" });
                }
                return View(model);
            }
            return Forbid();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CustomerModel model)
        {
            if (ModelState.IsValid && ValidateCustomer(model))
            {
                CustomerOrganisation customer = new CustomerOrganisation();
                model.UpdateCustomer(customer);
                customer.PriceListType = model.PriceListType;
                await _dbContext.AddAsync(customer);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { Id = customer.CustomerOrganisationId, Message = "Myndighet har skapats" });
            }
            return View(model);
        }

        private bool ValidateCustomer(CustomerModel model)
        {
            bool valid = true;
            //Test prefix
            if (_dbContext.CustomerOrganisations.Any(c =>
                 c.CustomerOrganisationId != model.CustomerId &&
                 c.OrganizationPrefix.Equals(model.OrganizationPrefix, StringComparison.InvariantCultureIgnoreCase)))
            {
                ModelState.AddModelError(nameof(model.OrganizationPrefix), $"Denna Namnprefix används redan i tjänsten.");
                valid = false;
            }
            return valid;
        }
    }
}
