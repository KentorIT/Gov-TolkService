using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.ApplicationAdministrator)]
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

        public IActionResult ListUsers(int id, IDataTablesRequest request)
        {
            // NEED TO APPLY SORT ORDER?
            var data = _dbContext.Users.Where(u => u.CustomerOrganisationId == id).Select(u => new DynamicUserListItemModel
            {
                Id = u.Id,
                FirstName = u.NameFirst,
                LastName = u.NameFamily,
                Email = u.Email
            });
            //HOW TO SEND FILTER VALUES?
            var filteredData = data;

            var sortColumn = request.Columns.Where(c => c.Sort != null).OrderBy(c => c.Sort.Order).FirstOrDefault();
            if (sortColumn != null)
            {
                data = data.OrderBy($"{sortColumn.Name} {(sortColumn.Sort.Direction == SortDirection.Ascending ? "ASC" : "DESC")}");
            }

            var dataPage = data.Skip(request.Start).Take(request.Length);

            // Response creation. To create your response you need to reference your request, to avoid
            // request/response tampering and to ensure response will be correctly created.
            var response = DataTablesResponse.Create(request, data.Count(), filteredData.Count(), dataPage);

            // Easier way is to return a new 'DataTablesJsonResult', which will automatically convert your
            // response to a json-compatible content, so DataTables can read it when received.
            return new DataTablesJsonResult(response, true);
        }

        public JsonResult UserColumnDefinition()
        {
            //THIS SHOULD BE RETRIEVED FROM A MORE CENTRALIZED PLACE, AND BY READING ATTRIBUTES FROM UserListItem
            return Json(Columns);
        }

        private static List<ColumnDefinition> Columns => new List<ColumnDefinition>()
            {
                new ColumnDefinition
                {
                    Name = "Id",
                    Data = "id",
                    Title = "",
                    Visible = false
                },
                new ColumnDefinition
                {
                    Name = "LastName",
                    Data = "lastName",
                    Title = "Efternamn",
                },
                new ColumnDefinition
                {
                    Name = "FirstName",
                    Data = "firstName",
                    Title = "Förnamn",
                },
                new ColumnDefinition
                {
                    Name = "Email",
                    Data = "email",
                    Title = "Epost"
                },
            };

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
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public string Title { get; set; }
        public bool Sortable { get; set; } = true;
        public bool Searchable { get; set; } = false;
        public bool Visible { get; set; } = true;
    }


}
