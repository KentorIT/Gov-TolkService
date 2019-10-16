using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
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
    [Authorize(Policies.CentralLocalAdminCustomer)]
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
                    await _userService.LogCustomerUnitUserUpdateAsync(model.LocalAdministrator, User.GetUserId());
                    await _dbContext.AddAsync(unit);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(View), new { id = unit.CustomerUnitId });
                }
            }
            return View(model);
        }

        public async Task<ActionResult> View(int id)
        {
            var unit = GetUnitToHandle(id);
            if (unit != null)
            {
                if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.View)).Succeeded)
                {
                    var model = new CustomerUnitModel
                    {
                        CustomerUnitId = id,
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
            }
            return Forbid();
        }

        public async Task<ActionResult> Edit(int id)
        {
            var unit = GetUnitToHandle(id);
            if (unit != null)
            {
                if ((await _authorizationService.AuthorizeAsync(User, unit, Policies.Edit)).Succeeded)
                {
                    var model = new CustomerUnitModel
                    {
                        CustomerUnitId = id,
                        Name = unit.Name,
                        Email = unit.Email,
                        IsActive = unit.IsActive,
                        IsCentralAdministrator = User.IsInRole(Roles.CentralAdministrator)
                    };
                    return View(model);
                }
            }
            return Forbid();
        }

        private CustomerUnit GetUnitToHandle(int id)
        {
            return _dbContext.CustomerUnits
               .Include(s => s.CreatedByUser)
               .Include(s => s.InactivatedByUser)
               .SingleOrDefault(cu => cu.CustomerUnitId == id);
        }

        //public IActionResult ListUsers(int id, IDataTablesRequest request)
        //{
        //    // NEED TO APPLY SORT ORDER?

        //    var unitUsersId = _dbContext.CustomerUnits
        //        .Include(cu => cu.CustomerUnitUsers).ThenInclude(cuu => cuu.User)
        //        .Where(cu => cu.CustomerUnitId == id).Single().CustomerUnitUsers
        //        .Where(cu => cu.UserId != User.GetUserId()).Select(cuu => cuu.User.Id);

        //    var data = _dbContext.Users.Include(u => u.CustomerUnits).Where(u => unitUsersId.Contains(u.Id)).Select(u => new DynamicUserListItemModel
        //    {
        //        Id = u.Id,
        //        CombinedId = u.Id + "_" + u.CustomerUnits.Where(cu => cu.CustomerUnitId == id).Single().CustomerUnitId,
        //        FirstName = u.NameFirst,
        //        LastName = u.NameFamily,
        //        Email = u.Email,
        //        IsActive = u.IsActive ? "Aktiv" : "Inaktiv",
        //        IsLocalAdmin = u.CustomerUnits.Any(cu => cu.CustomerUnitId == id && cu.IsLocalAdmin) ? "Ja" : "Nej"
        //    });
        //    //HOW TO SEND FILTER VALUES?
        //    var filteredData = data;

        //    var sortColumn = request.Columns.Where(c => c.Sort != null).OrderBy(c => c.Sort.Order).FirstOrDefault();
        //    if (sortColumn != null)
        //    {
        //        data = data.OrderBy($"{sortColumn.Name} {(sortColumn.Sort.Direction == SortDirection.Ascending ? "ASC" : "DESC")}");
        //    }

        //    var dataPage = data.Skip(request.Start).Take(request.Length);

        //    // Response creation. To create your response you need to reference your request, to avoid
        //    // request/response tampering and to ensure response will be correctly created.
        //    var response = DataTablesResponse.Create(request, data.Count(), filteredData.Count(), dataPage);

        //    // Easier way is to return a new 'DataTablesJsonResult', which will automatically convert your
        //    // response to a json-compatible content, so DataTables can read it when received.
        //    return new DataTablesJsonResult(response, true);
        //}

        //public JsonResult UserColumnDefinition()
        //{
        //    //THIS SHOULD BE RETRIEVED FROM A MORE CENTRALIZED PLACE, AND BY READING ATTRIBUTES FROM UserListItem
        //    return Json(Columns);
        //}

        //private static List<ColumnDefinition> Columns => new List<ColumnDefinition>()
        //    {
        //        new ColumnDefinition
        //        {
        //            Name = "Id",
        //            Data = "id",
        //            Title = "",
        //            Visible = false
        //        },
        //        new ColumnDefinition
        //        {
        //            Name = "CombinedId",
        //            Data = "combinedId",
        //            Title = "",
        //            Visible = false
        //        },
        //        new ColumnDefinition
        //        {
        //            Name = "LastName",
        //            Data = "lastName",
        //            Title = "Efternamn",
        //        },
        //        new ColumnDefinition
        //        {
        //            Name = "FirstName",
        //            Data = "firstName",
        //            Title = "Förnamn",
        //        },
        //        new ColumnDefinition
        //        {
        //            Name = "Email",
        //            Data = "email",
        //            Title = "Epost"
        //        },
        //        new ColumnDefinition
        //        {
        //            Name = "IsActive",
        //            Data = "isActive",
        //            Title = "Status"
        //        },
        //        new ColumnDefinition
        //        {
        //            Name = "IsLocalAdmin",
        //            Data = "isLocalAdmin",
        //            Title = "Lokal administratör"
        //        },
        //    };

        public async Task<ActionResult> Users(int id, string errorMessage = null, string message = null)
        {
            var unit = _dbContext.CustomerUnits.SingleOrDefault(cu => cu.CustomerUnitId == id);

            var unitUsersId = _dbContext.CustomerUnits
                .Include(cu => cu.CustomerUnitUsers).ThenInclude(cuu => cuu.User)
                .Where(cu => cu.CustomerUnitId == id).Single().CustomerUnitUsers
                .Select(cuu => cuu.User.Id);

            var users = _dbContext.Users.Include(u => u.CustomerUnits).Where(u => unitUsersId.Contains(u.Id)).Select(u => new DynamicUserListItemModel
            {
                Id = u.Id,
                CombinedId = u.Id + "_" + u.CustomerUnits.Where(cu => cu.CustomerUnitId == id).Single().CustomerUnitId,
                FirstName = u.NameFirst,
                LastName = u.NameFamily,
                Email = u.Email,
                IsActive = u.IsActive,
                IsLocalAdmin = u.CustomerUnits.Any(cu => cu.CustomerUnitId == id && cu.IsLocalAdmin) ? "Ja" : "Nej"
            });


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
                    ModelState.AddModelError(nameof(model.Name), $"Namnet används redan för en annan enhet för denna myndighet.");
                }
                else if (!IsUniqueEmail(model.Email, model.CustomerUnitId))
                {
                    ModelState.AddModelError(nameof(model.Email), $"E-postadressen används redan för en annan enhet för denna myndighet.");
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

        private bool IsUniqueEmail(string email, int? customerUnitId = null)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Email.ToSwedishUpper() == email.ToSwedishUpper() && u.CustomerUnitId != customerUnitId);
        }

        private bool IsUniqueName(string name, int? customerUnitId = null)
        {
            return !_dbContext.CustomerUnits.Any(u => u.CustomerOrganisationId == User.GetCustomerOrganisationId()
                && u.Name.ToSwedishUpper() == name.ToSwedishUpper() && u.CustomerUnitId != customerUnitId);
        }
    }
}
