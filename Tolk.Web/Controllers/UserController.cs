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
    [Authorize(Policies.SystemCentralLocalAdmin)]
    public class UserController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserService _userService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;
        private readonly HashService _hashService;

        public UserController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            ILogger<UserController> logger,
            RoleManager<IdentityRole<int>> roleManager,
            UserService userService,
            IAuthorizationService authorizationService,
            INotificationService notificationService,
            HashService hashService
)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
            _roleManager = roleManager;
            _userService = userService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
            _hashService = hashService;
        }

        public ActionResult List(UserFilterModel model)
        {
            if (model == null)
            {
                model = new UserFilterModel();
            }

            model.IsSystemAdministrator = User.IsInRole(Roles.SystemAdministrator) || User.IsInRole(Roles.ApplicationAdministrator);

            var customerId = User.TryGetCustomerOrganisationId();
            var brokerId = User.TryGetBrokerId();
            var users = _dbContext.Users.Where(u => !u.IsApiUser).Select(u => u);
            if (customerId.HasValue)
            {
                model.IsCustomer = true;
                users = users.Where(u => u.CustomerOrganisationId == customerId);
            }
            else if (brokerId.HasValue)
            {
                model.IsBroker = true;
                users = users.Where(u => u.BrokerId == brokerId);
            }
            else if (!model.IsSystemAdministrator)
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
                FilterModel = model,
                UserPageMode = new UserPageMode
                {
                    BackAction = nameof(List),
                    BackController = "User",
                    BackId = string.Empty
                }
            });
        }

        public async Task<ActionResult> View(int id, string bc = null, string ba = null, string bi = null)
        {
            var user = GetUserToHandle(id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.View)).Succeeded)
            {
                int centralAdministratorId = _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
                var model = new UserModel
                {
                    Id = id,
                    UserName = user.UserName,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    Email = user.Email,
                    PhoneWork = user.PhoneNumber ?? "-",
                    PhoneCellphone = user.PhoneNumberCellphone ?? "-",
                    IsOrganisationAdministrator = user.Roles.Any(r => r.RoleId == centralAdministratorId),
                    LastLoginAt = string.Format("{0:yyyy-MM-dd}", user.LastLoginAt) ?? "-",
                    Organisation = user.CustomerOrganisation?.Name ?? user.Broker?.Name ?? "-",
                    IsActive = user.IsActive,
                    UserPageMode = new UserPageMode
                    {
                        BackController = bc ?? BackController,
                        BackAction = ba ?? BackAction,
                        BackId = bi ?? BackId
                    }
                };

                return View(model);
            }
            return Forbid();
        }

        private string BackController => HttpContext.Request.Headers["Referer"].ToString().Contains("Customer") ? "Customer" : HttpContext.Request.Headers["Referer"].ToString().Contains("Unit") ? "Unit" : "User";

        private string BackAction => BackController == "User" ? "List" : BackController == "Unit" ? "Users" : "View";

        private string BackId
        {
            get
            {
                var referer = HttpContext.Request.Headers["Referer"].ToString();
                if (!(referer.Contains("Unit") || referer.Contains("Customer")))
                {
                    return string.Empty;
                }
                var id = referer.Split("/").Last();
                return id.Contains("?") ? id.Split("?").First() : id;
            }
        }

        public async Task<ActionResult> Edit(int id, string bc, string ba, string bi)
        {
            int centralAdministratorId = _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
            var user = _userManager.Users.Include(u => u.Roles)
                .Include(u => u.CustomerUnits).ThenInclude(cu => cu.CustomerUnit).SingleOrDefault(u => u.Id == id);
            IEnumerable<CustomerUnit> customerUnits = LoggedInCustomerUnits;

            var unitUsers = customerUnits?.Select(cu => new UnitUserModel
            {
                UserIsConnected = user.CustomerUnits.Any(c => c.CustomerUnitId == cu.CustomerUnitId),
                CustomerUnitId = cu.CustomerUnitId,
                IsActive = cu.IsActive,
                Name = cu.Name,
                IsLocalAdmin = user.CustomerUnitsLocalAdmin.Any(c => c.CustomerUnitId == cu.CustomerUnitId),
            });

            //get non editable list with unit users where editor user is not localadmin
            IEnumerable<UnitUserModel> nonEditableUnitUsers = null;
            if (unitUsers != null && LoggedInUserType == UserType.LocalAdministrator)
            {
                nonEditableUnitUsers = user.CustomerUnits.Where(c => !unitUsers.Select(uu => uu.CustomerUnitId).Contains(c.CustomerUnitId))
                    .Select(cu => new UnitUserModel
                    {
                        UserIsConnected = true,
                        CustomerUnitId = cu.CustomerUnitId,
                        IsActive = cu.CustomerUnit.IsActive,
                        Name = cu.CustomerUnit.Name,
                        IsLocalAdmin = cu.IsLocalAdmin
                    });
            }

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
                    IsOrganisationAdministrator = user.Roles.Any(r => r.RoleId == centralAdministratorId),
                    IsActive = user.IsActive,
                    UserType = LoggedInUserType,
                    UnitUsers = unitUsers?.OrderByDescending(uu => uu.UserIsConnected).ThenByDescending(uu => uu.IsLocalAdmin)
                        .ThenByDescending(uu => uu.IsActive).ThenBy(uu => uu.Name).ToList(),
                    NonEditableUnitUsers = nonEditableUnitUsers?.OrderBy(n => n.IsActive).ThenBy(n => n.Name).ToList(),
                    UserPageMode = new UserPageMode
                    {
                        BackController = bc,
                        BackAction = ba,
                        BackId = bi
                    }
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
                int centralAdministratorId = _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
                var user = _userManager.Users.Include(u => u.Roles).Include(u => u.CustomerUnits).SingleOrDefault(u => u.Id == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                {
                    //shouldn't we send editor user to LogOnUpdateAsync below?
                    await _userService.LogOnUpdateAsync(model.Id.Value);
                    user.NameFirst = model.NameFirst;
                    user.NameFamily = model.NameFamily;
                    user.PhoneNumber = model.PhoneWork;
                    user.PhoneNumberCellphone = model.PhoneCellphone;
                    if (user.IsActive && !model.IsActive)
                    {
                        await _userManager.UpdateSecurityStampAsync(user);
                    }
                    user.IsActive = model.IsActive;
                    if (model.IsOrganisationAdministrator && !user.Roles.Any(r => r.RoleId == centralAdministratorId))
                    {
                        await _userManager.AddToRoleAsync(user, Roles.CentralAdministrator);
                    }
                    else if (!model.IsOrganisationAdministrator && user.Roles.Any(r => r.RoleId == centralAdministratorId))
                    {
                        await _userManager.RemoveFromRoleAsync(user, Roles.CentralAdministrator);
                    }
                    if (model.UnitUsers != null)
                    {
                        List<CustomerUnitUser> unitsToRemove = new List<CustomerUnitUser>();
                        foreach (CustomerUnitUser cu in user.CustomerUnits)
                        {
                            var tempUnitUser = model.UnitUsers.Where(mu => mu.UserIsConnected).ToList().SingleOrDefault(c => c.CustomerUnitId == cu.CustomerUnitId);
                            //if still connected update IsLocalAdmin
                            if (tempUnitUser != null)
                            {
                                cu.IsLocalAdmin = tempUnitUser.IsLocalAdmin;
                            }
                            //else check if CustomerUnitId exixts in model list - if so remove it
                            else if (model.UnitUsers.Select(mu => mu.CustomerUnitId).Contains(cu.CustomerUnitId))
                            {
                                unitsToRemove.Add(cu);
                            }
                        }
                        //check if any new connected units that should be added
                        foreach (UnitUserModel uum in model.UnitUsers.Where(mu => mu.UserIsConnected && !user.CustomerUnits.Select(cu => cu.CustomerUnitId).Contains(mu.CustomerUnitId)))
                        {
                            user.CustomerUnits.Add(new CustomerUnitUser { IsLocalAdmin = uum.IsLocalAdmin, CustomerUnitId = uum.CustomerUnitId });
                        }
                        if (unitsToRemove.Any())
                        {
                            _dbContext.CustomerUnitUsers.RemoveRange(unitsToRemove);
                        }
                    }
                    await _userManager.UpdateAsync(user);
                }
            }
            return RedirectToAction(nameof(View), new { id = model.Id, bc = model.UserPageMode.BackController, ba = model.UserPageMode.BackAction, bi = model.UserPageMode.BackId });
        }

        public ActionResult Create(string bc, string ba, string bi, int? customerId = null, int? customerUnitId = null)
        {
            bool hasSelectedCustomer = false;
            string customerName = string.Empty;
            var customerOrganisationId = User.TryGetCustomerOrganisationId();
            if (!customerOrganisationId.HasValue && customerId.HasValue)
            {
                customerOrganisationId = customerId;
                hasSelectedCustomer = true;
                customerName = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == customerId).Name;
                //Set the OrganisationIdentifier, and make the ui just show the name of the organisation instead.
                //I.e. hidden field for OrganisationIdentifier and a display field for Organisation

            }
            IEnumerable<CustomerUnit> customerUnits = LoggedInCustomerUnits;
            List<UnitUserModel> unitUsers = null;
            if (customerUnits != null && customerUnits.Any())
            {
                customerUnits = customerUnitId.HasValue ? customerUnits.Where(cu => cu.CustomerUnitId == customerUnitId) : customerUnits;
                unitUsers = customerUnits.OrderByDescending(items => items.IsActive).ThenBy(items => items.Name).Select(cu =>
                    new UnitUserModel
                    {
                        IsActive = cu.IsActive,
                        IsLocalAdmin = false,
                        Name = cu.Name,
                        UserIsConnected = (customerUnitId.HasValue && customerUnitId == cu.CustomerUnitId) ? true : (customerUnits.Count() == 1 && LoggedInUserType == UserType.LocalAdministrator) ? true : false,
                        CustomerUnitId = cu.CustomerUnitId
                    }).OrderByDescending(items => items.UserIsConnected).ToList();
            }
            return View(new UserModel
            {
                UserType = LoggedInUserType,
                UnitUsers = unitUsers,
                HasSelectedOrganisation = hasSelectedCustomer,
                OrganisationIdentifier = hasSelectedCustomer ? $"{customerOrganisationId.ToString()}_{OrganisationType.GovernmentBody}" : null,
                Organisation = customerName,
                HasSelectedCustomerunit = customerUnitId.HasValue,
                UserPageMode = new UserPageMode
                {
                    BackController = bc,
                    BackAction = ba,
                    BackId = bi
                }
            });
        }

        private UserType LoggedInUserType => User.IsInRole(Roles.SystemAdministrator) || User.IsInRole(Roles.ApplicationAdministrator) ? UserType.SystemAdministrator
            : User.IsInRole(Roles.CentralAdministrator) ? UserType.OrganisationAdministrator : UserType.LocalAdministrator;

        private IEnumerable<CustomerUnit> LoggedInCustomerUnits => User.TryGetCustomerOrganisationId().HasValue ?
            User.IsInRole(Roles.CentralAdministrator) ?
            _dbContext.CustomerUnits.Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId()) :
            _dbContext.CustomerUnits.Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId()
            && cu.CustomerUnitUsers.Any(cuu => cuu.UserId == User.GetUserId() && cuu.IsLocalAdmin)) : null;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                bool serversideValid = true;
                if (!_userService.IsUniqueEmail(model.Email))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Denna e-postadress används redan i tjänsten.");
                }
                else if (LoggedInUserType == UserType.LocalAdministrator && !model.UnitUsers.Where(uu => uu.UserIsConnected).Any())
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Du måste koppla användaren till minst en enhet.");
                }
                if (!serversideValid)
                {
                    if (model.UnitUsers != null && model.UnitUsers.Any())
                    {
                        List<CustomerUnit> customerUnits = LoggedInCustomerUnits.ToList();

                        var unitUsers = (from unitUser in model.UnitUsers
                                         join customerUnit in customerUnits on unitUser.CustomerUnitId
                                         equals customerUnit.CustomerUnitId
                                         select
                                         new UnitUserModel
                                         {
                                             IsActive = customerUnit.IsActive,
                                             IsLocalAdmin = unitUser.IsLocalAdmin,
                                             Name = customerUnit.Name,
                                             UserIsConnected = unitUser.UserIsConnected,
                                             CustomerUnitId = customerUnit.CustomerUnitId
                                         }).ToList();
                        model.UnitUsers = unitUsers;
                    }
                    model.UserType = LoggedInUserType;
                }
                else
                {
                    using (var trn = await _dbContext.Database.BeginTransactionAsync())
                    {
                        var additionalRoles = new List<string>();
                        var organisationPrefix = string.Empty;
                        int? customerId = null;
                        int? brokerId = null;
                        if (!User.IsInRole(Roles.SystemAdministrator))
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
                                        additionalRoles.Add(Roles.SystemAdministrator);
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
                        List<CustomerUnitUser> unitUsers = new List<CustomerUnitUser>();

                        if (model.UnitUsers != null)
                        {
                            foreach (UnitUserModel um in model.UnitUsers.Where(mu => mu.UserIsConnected))
                            {
                                unitUsers.Add(new CustomerUnitUser { IsLocalAdmin = um.IsLocalAdmin, CustomerUnitId = um.CustomerUnitId });
                            }
                        }

                        var user = new AspNetUser(model.Email,
                            _userService.GenerateUserName(model.NameFirst, model.NameFamily, organisationPrefix),
                            model.NameFirst,
                            model.NameFamily)
                        {
                            CustomerOrganisationId = customerId,
                            BrokerId = brokerId,
                            CustomerUnits = unitUsers.Any() ? unitUsers : null
                        };
                        if (model.IsOrganisationAdministrator)
                        {
                            additionalRoles.Add(Roles.CentralAdministrator);
                        }
                        var result = await _userManager.CreateAsync(user);

                        if (result.Succeeded)
                        {
                            if (additionalRoles.Any())
                            {
                                //Make another system administrator user
                                var roleResult = await _userManager.AddToRolesAsync(user, additionalRoles);
                                if (!roleResult.Succeeded)
                                {
                                    throw new NotSupportedException("Failed to add user, trying to add roles.");
                                }
                            }
                            await _userService.SendInviteAsync(user);
                            await _userService.LogCreateAsync(user.Id, User.GetUserId());

                            trn.Commit();
                            return RedirectToAction(model.UserPageMode.BackAction, model.UserPageMode.BackController, new { id = model.UserPageMode.BackId });
                        }
                        model.ErrorMessage = GetErrors(result);
                    }
                }
            }
            return View(model);
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public ActionResult ViewOrganisationSettings(string message)
        {
            var brokerId = User.TryGetBrokerId();
            if (brokerId != null)
            {
                var apiUser = _dbContext.Users
                    .Include(u => u.Claims)
                    .Include(u => u.NotificationSettings)
                    .Include(u => u.Broker)
                    .SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                if (apiUser != null)
                {
                    return View(new OrganisationSettingsModel
                    {
                        Message = message,
                        UserName = apiUser.UserName,
                        Email = apiUser.Email,
                        CertificateSerialNumber = apiUser.Claims.SingleOrDefault(c => c.ClaimType == "CertificateSerialNumber")?.ClaimValue,
                        UseApiKeyAuthentication = apiUser.Claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"),
                        UseCertificateAuthentication = apiUser.Claims.Any(c => c.ClaimType == "UseCertificateAuthentication"),
                        OrganisationNumber = apiUser.Broker.OrganizationNumber,
                        NotificationSettings = apiUser.NotificationSettings.Select(s => new NotificationSettingsDetailsModel
                        {
                            Type = s.NotificationType,
                            Channel = s.NotificationChannel,
                            ContactInformation = s.ConnectionInformation
                        })
                    });
                }
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public ActionResult EditNotificationSettings()
        {
            var brokerId = User.TryGetBrokerId();
            if (brokerId != null)
            {
                var apiUser = _dbContext.Users
                    .Include(u => u.NotificationSettings)
                    .SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                if (apiUser != null)
                {
                    return View(GetAvailableNotifications(apiUser.NotificationSettings.Select(s => s)).ToList());
                }
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> EditNotificationSettings(IEnumerable<NotificationSettingsModel> model)
        {
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
                        if (ModelState.IsValid)
                        {
                            await _userService.LogOnNotificationSettingsUpdateAsync(apiUser.Id, User.GetUserId());
                            foreach (var setting in model)
                            {
                                UpdateNotificationSetting(apiUser, setting.UseEmail, setting.Type, NotificationChannel.Email, setting.SpecificEmail);
                                UpdateNotificationSetting(apiUser, setting.UseWebHook, setting.Type, NotificationChannel.Webhook, setting.WebHookUrl);
                            }
                            await _dbContext.SaveChangesAsync();
                            transaction.Complete();
                            _notificationService.FlushNotificationSettings();
                            return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                        }
                        return View(model);
                    }
                }
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public ActionResult ChangeApiKey()
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
                    return View();
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> ChangeApiKey(ChangeApiKeyModel model)
        {
            if (ModelState.IsValid)
            {
                //Save all Claims and Notification settings and stuff here...
                var brokerId = User.TryGetBrokerId();
                if (brokerId != null)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                    {
                        ModelState.AddModelError(nameof(model.CurrentPassword), "Lösenordet som angivits är felaktigt.");
                        return View(model);
                    }
                    using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var apiUser = _dbContext.Users.SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                        if (apiUser != null)
                        {
                            var secretClaim = (await _userManager.GetClaimsAsync(apiUser)).SingleOrDefault(c => c.Type == "Secret");
                            if (secretClaim != null)
                            {
                                await _userManager.RemoveClaimAsync(apiUser, secretClaim);
                            }
                            var saltClaim = (await _userManager.GetClaimsAsync(apiUser)).SingleOrDefault(c => c.Type == "Salt");
                            if (saltClaim != null)
                            {
                                await _userManager.RemoveClaimAsync(apiUser, saltClaim);
                            }

                            var salt = _hashService.CreateSalt(32);
                            await _userManager.AddClaimAsync(apiUser, new Claim("Secret", _hashService.GenerateHash(model.ApiKey, salt)));
                            await _userManager.AddClaimAsync(apiUser, new Claim("Salt", salt));
                            transaction.Complete();
                            return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                        }
                    }
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> DisconnectUser(string combinedId)
        {
            var unitUser = GetUnitUser(combinedId);
            var user = GetUserToHandle(unitUser.UserId);
            bool removeOneSelfAsLocalAdmin = user.Id == User.GetUserId() && !User.IsInRole(Roles.CentralAdministrator);
            if ((await _authorizationService.AuthorizeAsync(User, unitUser, Policies.Edit)).Succeeded)
            {
                if (unitUser != null)
                {
                    if (IfLastUserWithLocalAdmin(unitUser))
                    {
                        return RedirectToAction("Users", "Unit", new { id = unitUser.CustomerUnitId, errorMessage = $"Det går inte att ta bort {user.FullName} som lokal administratör då enheten måste ha minst en användare som lokal administratör. Gör en annan användare till lokal administratör före borttag." });
                    }
                    await _userService.LogCustomerUnitUserUpdateAsync(unitUser.UserId, User.GetUserId());
                    _dbContext.CustomerUnitUsers.Remove(unitUser);
                    _dbContext.SaveChanges();
                    return RedirectToAction("Users", "Unit", new { id = unitUser.CustomerUnitId, message = removeOneSelfAsLocalAdmin ? 
                        "Du är nu bortkopplad från enheten, dina rättigheter för enheten försvinner om fem minuter." : 
                        $"{user.FullName} är bortkopplad från enheten" });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> ChangeLocalAdmin(string combinedId)
        {
            var unitUser = GetUnitUser(combinedId);
            var user = GetUserToHandle(unitUser.UserId);
            bool removeOneSelfAsLocalAdmin = user.Id == User.GetUserId() && !User.IsInRole(Roles.CentralAdministrator) && unitUser.IsLocalAdmin;
            if ((await _authorizationService.AuthorizeAsync(User, unitUser, Policies.Edit)).Succeeded)
            {
                if (unitUser != null)
                {
                    if (IfLastUserWithLocalAdmin(unitUser))
                    {
                        return RedirectToAction("Users", "Unit", new { id = unitUser.CustomerUnitId, errorMessage = $"Det går inte att ta bort {user.FullName} som lokal administratör då enheten måste ha minst en användare som lokal administratör. Gör en annan användare till lokal administratör före borttag." });
                    }
                    await _userService.LogCustomerUnitUserUpdateAsync(unitUser.UserId, User.GetUserId());
                    unitUser.IsLocalAdmin = !unitUser.IsLocalAdmin;
                    _dbContext.SaveChanges();
                }
                return RedirectToAction("Users", "Unit", new { id = unitUser.CustomerUnitId, message = removeOneSelfAsLocalAdmin ? 
                    "Du är inte längre lokal administratör för enheten, dina administratörsrättigheter för enheten försvinner om fem minuter." : 
                    $"Lokal administratör ändrad för {user.FullName}" });
            }
            return Forbid();
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> ConnectUserToUnit(ConnectUserUnitModel model)
        {
            if (!model.ConnectUserId.HasValue)
            {
                return RedirectToAction("Users", "Unit", new { id = model.CustomerUnitId, errorMessage = "Du valde ingen befintlig användare att koppla till enheten, försök igen" });
            }
            var user = GetUserToHandle(model.ConnectUserId.Value);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.View)).Succeeded)
            {
                await _userService.LogCustomerUnitUserUpdateAsync(model.ConnectUserId.Value, User.GetUserId());
                _dbContext.CustomerUnitUsers.Add(new CustomerUnitUser { CustomerUnitId = model.CustomerUnitId, UserId = model.ConnectUserId.Value, IsLocalAdmin = model.IsLocalAdministrator });
                _dbContext.SaveChanges();
                return RedirectToAction("Users", "Unit", new { id = model.CustomerUnitId, message = $"{user.FullName} är nu kopplad till enheten" });
            }
            return Forbid();
        }

        private AspNetUser GetUserToHandle(int userId)
        {
            return _userManager.Users
            .Include(u => u.Roles)
            .Include(u => u.CustomerOrganisation)
            .Include(u => u.Broker)
            .SingleOrDefault(u => u.Id == userId);
        }

        private CustomerUnitUser GetUnitUser(string combinedId)
        {
            int userId = Convert.ToInt32(combinedId.Split("_")[0]);
            int customerUnitId = Convert.ToInt32(combinedId.Split("_")[1]);
            return _dbContext.CustomerUnitUsers.Include(cuu => cuu.CustomerUnit)
                .Where(cu => cu.CustomerUnitId == customerUnitId && cu.UserId == userId).Single();
        }

        private bool IfLastUserWithLocalAdmin(CustomerUnitUser unitUser)
        {
            return unitUser.IsLocalAdmin && !_dbContext.CustomerUnitUsers.Where(cu => cu.CustomerUnitId == unitUser.CustomerUnitId && cu.UserId != unitUser.UserId && cu.IsLocalAdmin).Any();
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public ActionResult EditOrganisationSettings()
        {
            var brokerId = User.TryGetBrokerId();
            if (brokerId != null)
            {
                var apiUser = _dbContext.Users
                    .Include(u => u.Claims)
                    .Include(u => u.NotificationSettings)
                    .Include(u => u.Broker)
                    .SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                if (apiUser != null)
                {
                    return View(new OrganisationSettingsModel
                    {
                        UserName = apiUser.UserName,
                        Email = apiUser.Email,
                        CertificateSerialNumber = apiUser.Claims.SingleOrDefault(c => c.ClaimType == "CertificateSerialNumber")?.ClaimValue,
                        UseApiKeyAuthentication = apiUser.Claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"),
                        UseCertificateAuthentication = apiUser.Claims.Any(c => c.ClaimType == "UseCertificateAuthentication"),
                        OrganisationNumber = apiUser.Broker.OrganizationNumber
                    });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = Roles.CentralAdministrator)]
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
                            .SingleOrDefault(u => u.IsApiUser && u.BrokerId == brokerId);
                        if (apiUser != null)
                        {
                            await _userService.LogOnUpdateAsync(apiUser.Id, User.GetUserId());
                            var broker = _dbContext.Brokers.Single(b => b.BrokerId == brokerId);
                            if (apiUser.NormalizedEmail != model.Email.ToUpper())
                            {
                                apiUser.Email = model.Email;
                                apiUser.NormalizedEmail = model.Email.ToUpper();
                                broker.EmailAddress = model.Email;
                            }
                            broker.OrganizationNumber = model.OrganisationNumber;
                            var brokerUser = await _userManager.FindByNameAsync(apiUser.UserName);
                            //Clear all Claims, and resave them...
                            var claims = await _userManager.GetClaimsAsync(apiUser);
                            var claim = claims.SingleOrDefault(c => c.Type == nameof(model.UseApiKeyAuthentication));
                            if (claim != null)
                            {
                                await _userManager.RemoveClaimAsync(brokerUser, claim);
                            }
                            if (model.UseApiKeyAuthentication)
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim(nameof(model.UseApiKeyAuthentication), DateTime.Now.ToShortDateString()));
                            }
                            claim = claims.SingleOrDefault(c => c.Type == nameof(model.UseCertificateAuthentication));
                            if (claim != null)
                            {
                                await _userManager.RemoveClaimAsync(brokerUser, claim);
                            }
                            if (model.UseCertificateAuthentication)
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim(nameof(model.UseCertificateAuthentication), DateTime.Now.ToShortDateString()));
                            }
                            claim = claims.SingleOrDefault(c => c.Type == nameof(model.CertificateSerialNumber));
                            if (claim != null)
                            {
                                await _userManager.RemoveClaimAsync(brokerUser, claim);
                            }
                            if (!string.IsNullOrWhiteSpace(model.CertificateSerialNumber))
                            {
                                await _userManager.AddClaimAsync(brokerUser, new Claim(nameof(model.CertificateSerialNumber), model.CertificateSerialNumber));
                            }
                            await _dbContext.SaveChangesAsync();
                            transaction.Complete();
                            return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                        }
                    }
                }
            }
            return View(model);
        }

        private void UpdateNotificationSetting(AspNetUser apiUser, bool isActive, NotificationType type, NotificationChannel channel, string connectionInformation)
        {
            var setting = apiUser.NotificationSettings.SingleOrDefault(s => s.NotificationType == type && s.NotificationChannel == channel);
            if (isActive)
            {
                if (setting == null)
                {
                    setting = new UserNotificationSetting
                    {
                        NotificationChannel = channel,
                        NotificationType = type
                    };
                    apiUser.NotificationSettings.Add(setting);
                }
                setting.ConnectionInformation = connectionInformation;
            }
            else if (setting != null)
            {
                _dbContext.UserNotificationSettings.Remove(setting);
            }
        }

        private IEnumerable<NotificationSettingsModel> GetAvailableNotifications(IEnumerable<UserNotificationSetting> settings)
        {
            foreach (var val in Enum.GetValues(typeof(NotificationType)).OfType<NotificationType>())
            {
                var emailSettings = settings.SingleOrDefault(s => s.NotificationType == val && s.NotificationChannel == NotificationChannel.Email);
                var webhookSettings = settings.SingleOrDefault(s => s.NotificationType == val && s.NotificationChannel == NotificationChannel.Webhook);
                yield return new NotificationSettingsModel
                {
                    Type = val,
                    UseEmail = emailSettings != null,
                    SpecificEmail = emailSettings?.ConnectionInformation,
                    UseWebHook = webhookSettings != null,
                    WebHookUrl = webhookSettings?.ConnectionInformation,
                };
            }
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
