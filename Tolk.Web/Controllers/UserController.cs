using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
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
            var users = _dbContext.Users.Where(u => !u.IsApiUser).Select(u => u);
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
                    UserName = user.UserName,
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
                    UserName = user.UserName,
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserModel model)
        {
            if (ModelState.IsValid)
            {
                int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
                var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                {
                    await _userService.LogOnUpdateAsync(model.Id.Value);
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
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                using (var trn = await _dbContext.Database.BeginTransactionAsync())
                {
                    var additionalRoles = new List<string>();
                    var organisationPrefix = string.Empty;
                    int? customerId = null;
                    int? brokerId = null;
                    if (!User.IsInRole(Roles.Admin))
                    {
                        customerId = User.TryGetCustomerOrganisationId();
                        brokerId = User.TryGetBrokerId();
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(model.OrganisationIdentifier))
                        {
                            var org = model.OrganisationIdentifier.Split("_");
                            var id = int.Parse(org.First());
                            var type = Enum.Parse<OrganisationType>(org.Last());
                            switch (type)
                            {
                                case OrganisationType.GovernmentBody:
                                    customerId = id;
                                    break;
                                case OrganisationType.Broker:
                                    brokerId = id;
                                    break;
                                case OrganisationType.Owner:
                                    additionalRoles.Add(Roles.Admin);
                                    organisationPrefix = "KamK";
                                    break;
                                default:
                                    throw new NotSupportedException($"{type.GetDescription()} is not a supported {nameof(OrganisationType)} when creating users.");
                            }
                        }
                    }
                    if (brokerId.HasValue)
                    {
                        organisationPrefix = _dbContext.Brokers.Single(c => c.BrokerId == brokerId).OrganizationPrefix;
                    }
                    if (customerId.HasValue)
                    {
                        organisationPrefix = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == customerId).OrganizationPrefix;
                    }

                    var user = new AspNetUser(model.Email,
                        _userService.GenerateUserName(model.NameFirst, model.NameFamily, organisationPrefix),
                        model.NameFirst,
                        model.NameFamily)
                    {
                        CustomerOrganisationId = customerId,
                        BrokerId = brokerId
                    };
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

                        await _userService.LogCreateAsync(user.Id, User.GetUserId());

                        trn.Commit();
                        return RedirectToAction(nameof(View), new { id = user.Id });
                    }
                    model.ErrorMessage = GetErrors(result);
                }
            }

            return View(model);
        }

        [Authorize(Roles = Roles.SuperUser)]
        public ActionResult EditOrganisationSettings()
        {
            var brokerId = User.TryGetBrokerId();
            if (brokerId != null)
            {
                var apiUser = _dbContext.Users
                    .Include(u => u.Claims)
                    .Include(u => u.NotificationSettings)
                    .SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                if (apiUser != null)
                {
                    return View(new OrganisationSettingsModel
                    {
                        UserName = apiUser.UserName,
                        Email = apiUser.Email,
                        CertificateSerialNumber = apiUser.Claims.SingleOrDefault(c => c.ClaimType == "CertSerialNumber")?.ClaimValue,
                        ApiKey = apiUser.Claims.SingleOrDefault(c => c.ClaimType == "Secret")?.ClaimValue,
                        UseApiKeyAuthentication = apiUser.Claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"),
                        UseCertificateAuthentication = apiUser.Claims.Any(c => c.ClaimType == "UseCertificateAuthentication"),
                        UseWebHook = apiUser.NotificationSettings.Any(n => n.NotificationChannel == NotificationChannel.Webhook && n.NotificationType == NotificationType.RequestCreated),
                        RequestCreatedWebHook = apiUser.NotificationSettings.SingleOrDefault(n => n.NotificationChannel == NotificationChannel.Webhook && n.NotificationType == NotificationType.RequestCreated)?.ConnectionInformation
                    });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = Roles.SuperUser)]
        public async Task<ActionResult> EditOrganisationSettings(OrganisationSettingsModel model)
        {
            if (ModelState.IsValid)
            {
                //Save all Claims and Notification settings and stuff here...
                var brokerId = User.TryGetBrokerId();
                if (brokerId != null)
                {
                    using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var apiUser = _dbContext.Users
                            .Include(u => u.NotificationSettings)
                            .SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                        if (apiUser != null)
                        {
                            await _userService.LogOnUpdateAsync(apiUser.Id, User.GetUserId());
                            if (apiUser.NormalizedEmail != model.Email.ToUpper())
                            {
                                apiUser.Email = model.Email;
                                apiUser.NormalizedEmail = model.Email.ToUpper();
                                var broker = _dbContext.Brokers.Single(b => b.BrokerId == brokerId);
                                broker.EmailAddress = model.Email;
                            }
                            var brokerUser = await _userManager.FindByNameAsync(apiUser.UserName);
                            //Clear all Claims, and resave them...
                            await _userManager.RemoveClaimsAsync(brokerUser, await _userManager.GetClaimsAsync(brokerUser));
                            if (model.UseApiKeyAuthentication)
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim(nameof(model.UseApiKeyAuthentication), DateTime.Now.ToShortDateString()));
                            }
                            if (model.UseCertificateAuthentication)
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim("UseCertificateAuthentication", DateTime.Now.ToShortDateString()));
                            }
                            if (!string.IsNullOrWhiteSpace(model.CertificateSerialNumber))
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim("CertSerialNumber", model.CertificateSerialNumber));
                            }
                            if (!string.IsNullOrWhiteSpace(model.ApiKey))
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim("Secret", model.ApiKey));
                            }
                            var hookInfo = apiUser.NotificationSettings.SingleOrDefault(n => n.NotificationChannel == NotificationChannel.Webhook && n.NotificationType == NotificationType.RequestCreated);
                            if (model.UseWebHook)
                            {
                                if (hookInfo != null)
                                {
                                    hookInfo.ConnectionInformation = model.RequestCreatedWebHook;
                                }
                                else
                                {
                                    apiUser.NotificationSettings.Add(new UserNotificationSetting
                                    {
                                        NotificationType = NotificationType.RequestCreated,
                                        NotificationChannel = NotificationChannel.Webhook,
                                        ConnectionInformation = model.RequestCreatedWebHook
                                    });
                                }
                            }
                            else if (hookInfo != null)
                            {
                                apiUser.NotificationSettings.Remove(hookInfo);
                            }
                            _dbContext.SaveChanges();
                            transaction.Complete();
                            return RedirectToAction(nameof(HomeController.Index), "Home", new { message = "Ändringarna sparades" });
                        }
                    }
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
