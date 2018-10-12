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
    [Authorize(Roles = Roles.Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<InterpreterController> _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserService _userService;

        public UserController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            ILogger<InterpreterController> logger,
            RoleManager<IdentityRole<int>> roleManager,
            UserService userService
)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
            _roleManager = roleManager;
            _userService = userService;
        }

        public ActionResult List(UserFilterModel model)
        {
            var users = _dbContext.Users.Select(u => u);
            if (model != null)
            {
                users = model.Apply(users, _roleManager.Roles.Select(r => new RoleMap { Id = r.Id, Name = r.Name }).ToList());
            }
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

        public ActionResult View(int id)
        {
            var user = _userManager.Users
                .Include(u => u.Roles)
                .Include(u => u.CustomerOrganisation)
                .Include(u => u.Broker)
                .SingleOrDefault(u => u.Id == id);
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

        public ActionResult Create()
        {
            return View(new UserModel());
        }

        public ActionResult Edit(int id)
        {
            int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
            var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == id);

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

        [HttpPost]
        public async Task<ActionResult> Edit(UserModel model)
        {
            int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
            var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == model.Id);
            if (user is null)
            {
                user = new AspNetUser(model.Email);
            }
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
            return RedirectToAction(nameof(View), model);
        }

        [HttpPost]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                using (var trn = await _dbContext.Database.BeginTransactionAsync())
                {
                    var user = new AspNetUser(model.Email)
                    {
                        NameFirst = model.NameFirst,
                        NameFamily = model.NameFamily,
                        PhoneNumber = model.PhoneWork,
                        PhoneNumberCellphone = model.PhoneCellphone,
                        IsActive = model.IsActive
                    };
                    var additionalRoles = new List<string>();
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
                            default:
                                throw new NotSupportedException($"{type.GetDescription()} is not a supported {nameof(OrganisationType)} when creating users.");
                        }
                        if (model.IsSuperUser)
                        {
                            additionalRoles.Add(Roles.SuperUser);
                        }
                    }
                    else
                    {
                        additionalRoles.Add(Roles.Admin);
                    }
                    var result = await _userManager.CreateAsync(user);
                    if (additionalRoles.Any())
                    {
                        //Make another admin user
                        var roleResult = await _userManager.AddToRolesAsync(user, additionalRoles);
                        if (!result.Succeeded)
                        {
                            throw new NotSupportedException("Failed to add user, trying to add roles.");
                        }
                    }

                    if (result.Succeeded)
                    {
                        if (model.IsSuperUser) // And was added to an org, octherwize it is an admin?
                        {
                            await _userManager.AddToRoleAsync(user, Roles.SuperUser);
                        }
                        await _userService.SendInviteAsync(user);

                        trn.Commit();
                        return RedirectToAction(nameof(View), new { id = user.Id });
                    }
                    //AddErrors(result);
                }
            }
            return View(model);
        }
    }
}
