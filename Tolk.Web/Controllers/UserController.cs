using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
    [Authorize(Roles = Roles.AdminRoles)]
    public class UserController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserService _userService;
        private readonly IAuthorizationService _authorizationService;

        public UserController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            ILogger<UserController> logger,
            RoleManager<IdentityRole<int>> roleManager,
            UserService userService,
            IAuthorizationService authorizationService
)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
            _roleManager = roleManager;
            _userService = userService;
            _authorizationService = authorizationService;
        }

        public ActionResult List(UserFilterModel model)
        {
            if (model == null)
            {
                model = new UserFilterModel();
            }
            
            model.IsSystemAdministrator = User.IsInRole(Roles.Admin);

            var customerId = User.TryGetCustomerOrganisationId();
            var brokerId = User.TryGetBrokerId();
            var users = _dbContext.Users.Select(u => u);
            if (customerId.HasValue)
            {
                users = users.Where(u => u.CustomerOrganisationId == customerId);
            }
            else if (brokerId.HasValue)
            {
                users = users.Where(u => u.BrokerId == brokerId);
            }
            else if (!User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }
            users = model.Apply(users, _roleManager.Roles.Select(r => new RoleMap { Id = r.Id, Name = r.Name }).ToList());
            return View(new UserListModel
            {
                Items = users.Select(u => new UserListItemModel
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Name = u.FullName,
                    Organisation = u.CustomerOrganisation.Name ?? u.Broker.Name ?? "-",
                    LastLoginAt = string.Format("{0:yyyy-MM-dd}", u.LastLoginAt) ?? "-",
                    IsActive = u.IsActive
                }),
                FilterModel = model
            });
        }

        public async Task<ActionResult> View(int id)
        {
            var user = _userManager.Users
            .Include(u => u.Roles)
            .Include(u => u.CustomerOrganisation)
            .Include(u => u.Broker)
            .SingleOrDefault(u => u.Id == id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.View)).Succeeded)
            {
                int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
                var model = new UserModel
                {
                    Id = id,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    Email = user.Email,
                    PhoneWork = user.PhoneNumber ?? "-",
                    PhoneCellphone = user.PhoneNumberCellphone ?? "-",
                    IsSuperUser = user.Roles.Any(r => r.RoleId == superUserId),
                    LastLoginAt = string.Format("{0:yyyy-MM-dd}", user.LastLoginAt) ?? "-",
                    Organisation = user.CustomerOrganisation?.Name ?? user.Broker?.Name ?? "-",
                    IsActive = user.IsActive
                };

                return View(model);
            }
            return Forbid();
        }

        public async Task<ActionResult> Edit(int id)
        {
            int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
            var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
            {
                var model = new UserModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    PhoneWork = user.PhoneNumber,
                    PhoneCellphone = user.PhoneNumberCellphone,
                    IsSuperUser = user.Roles.Any(r => r.RoleId == superUserId),
                    IsActive = user.IsActive

                };
                return View(model);
            }
            return Forbid();
        }

        [HttpPost]
        public async Task<ActionResult> Edit(UserModel model)
        {
            if (ModelState.IsValid)
            {
                int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
                var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                {
                    user.NameFirst = model.NameFirst;
                    user.NameFamily = model.NameFamily;
                    user.PhoneNumber = model.PhoneWork;
                    user.PhoneNumberCellphone = model.PhoneCellphone;
                    user.IsActive = model.IsActive;
                    if (model.IsSuperUser && !user.Roles.Any(r => r.RoleId == superUserId))
                    {
                        await _userManager.AddToRoleAsync(user, Roles.SuperUser);
                    }
                    else if (!model.IsSuperUser && user.Roles.Any(r => r.RoleId == superUserId))
                    {
                        await _userManager.RemoveFromRoleAsync(user, Roles.SuperUser);
                    }
                    await _userManager.UpdateAsync(user);
                }
            }
            return RedirectToAction(nameof(View), model);
        }

        public ActionResult Create()
        {
            return View(new UserModel { EditorIsSystemAdministrator = User.IsInRole(Roles.Admin) });
        }

        [HttpPost]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                var customerId = User.TryGetCustomerOrganisationId();
                var brokerId = User.TryGetBrokerId();

                using (var trn = await _dbContext.Database.BeginTransactionAsync())
                {
                    var user = new AspNetUser(model.Email)
                    {
                        NameFirst = model.NameFirst,
                        NameFamily = model.NameFamily,
                        PhoneNumber = model.PhoneWork,
                        PhoneNumberCellphone = model.PhoneCellphone,
                        IsActive = true,
                        CustomerOrganisationId = customerId,
                        BrokerId = brokerId
                    };
                    var additionalRoles = new List<string>();
                    if (User.IsInRole(Roles.Admin))
                    {
                        if (!string.IsNullOrWhiteSpace(model.OrganisationIdentifier))
                        {
                            var org = model.OrganisationIdentifier.Split("_");
                            var id = int.Parse(org.First());
                            var type = Enum.Parse<OrganisationType>(org.Last());
                            switch (type)
                            {
                                case OrganisationType.GovernmentBody:
                                    user.CustomerOrganisationId = id;
                                    break;
                                case OrganisationType.Broker:
                                    user.BrokerId = id;
                                    break;
                                case OrganisationType.Owner:
                                    additionalRoles.Add(Roles.Admin);
                                    break;
                                default:
                                    throw new NotSupportedException($"{type.GetDescription()} is not a supported {nameof(OrganisationType)} when creating users.");
                            }
                        }
                    }
                    if (model.IsSuperUser)
                    {
                        additionalRoles.Add(Roles.SuperUser);
                    }
                    var result = await _userManager.CreateAsync(user);

                    if (result.Succeeded)
                    {
                        if (additionalRoles.Any())
                        {
                            //Make another admin user
                            var roleResult = await _userManager.AddToRolesAsync(user, additionalRoles);
                            if (!roleResult.Succeeded)
                            {
                                throw new NotSupportedException("Failed to add user, trying to add roles.");
                            }
                        }
                        await _userService.SendInviteAsync(user);

                        trn.Commit();
                        return RedirectToAction(nameof(View), new { id = user.Id });
                    }
                    model.ErrorMessage = GetErrors(result);
                }
            }

            return View(model);
        }

        private string GetErrors(IdentityResult result)
        {
            string errors = string.Empty;
            foreach (var error in result.Errors)
            {
                errors += error.Description + "\n";
            }
            return errors;
        }

        protected string GetErrorMessages()
        {
            string errorMessage = string.Empty;
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                errorMessage += error.ErrorMessage + "\n";
            }
            return errorMessage;
        }
    }
}
