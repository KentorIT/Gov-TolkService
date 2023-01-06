using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkDbContext _dbContext;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserService _userService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;
        private readonly CacheService _cacheService;
        private readonly TolkOptions _options;

        public UserController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            RoleManager<IdentityRole<int>> roleManager,
            UserService userService,
            IAuthorizationService authorizationService,
            INotificationService notificationService,
            CacheService cacheService,
            IOptions<TolkOptions> options
)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _roleManager = roleManager;
            _userService = userService;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
            _options = options?.Value;
            _cacheService = cacheService;
        }
        private int ImpersonatorRoleId => _roleManager.Roles.Single(r => r.Name == Roles.Impersonator).Id;
        private int CentralAdministratorRoleId => _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
        private int CentralOrderHandlerRoleId => _roleManager.Roles.Single(r => r.Name == Roles.CentralOrderHandler).Id;
        private int ApplicationAdministratorRoleId => _roleManager.Roles.Single(r => r.Name == Roles.ApplicationAdministrator).Id;
        private int SystemAdministratorRoleId => _roleManager.Roles.Single(r => r.Name == Roles.SystemAdministrator).Id;

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public ActionResult List()
        {
            UserFilterModel model = new UserFilterModel();

            var customerId = User.TryGetCustomerOrganisationId();
            var brokerId = User.TryGetBrokerId();
            model.IsBroker = brokerId.HasValue;
            model.IsCustomer = customerId.HasValue;
            model.UserType = HighestLevelLoggedInUserType;
            return View(new UserListModel
            {
                FilterModel = model,
                UserPageMode = new UserPageMode
                {
                    BackAction = nameof(List),
                    BackController = "User",
                    BackId = string.Empty
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ListUsers(IDataTablesRequest request)
        {
            var model = new UserFilterModel();
            await TryUpdateModelAsync(model);
            var users = _dbContext.Users.Where(u => !u.IsApiUser).Select(u => u);
            var customerId = User.TryGetCustomerOrganisationId();
            var brokerId = User.TryGetBrokerId();

            model.UserType = HighestLevelLoggedInUserType;
            if (customerId.HasValue)
            {
                users = users.Where(u => u.CustomerOrganisationId == customerId);
            }
            else if (brokerId.HasValue)
            {
                users = users.Where(u => u.BrokerId == brokerId);
            }
            else if (!IsSysOrAppAdmin)
            {
                return Forbid();
            }
            return AjaxDataTableHelper.GetData(request, users.Count(), model.Apply(users, _roleManager.Roles.Select(r => new RoleMap { Id = r.Id, Name = r.Name })), x => x.Select(u => new UserListItemModel
            {
                UserId = u.Id,
                Email = u.Email,
                Name = $"{u.NameFamily}, {u.NameFirst}",
                Organisation = u.CustomerOrganisation.Name ?? u.Broker.Name ?? "-",
                LastLoginAt = "{0:yyyy-MM-dd}".FormatSwedish(u.LastLoginAt) ?? "-",
                IsActive = u.IsActive
            }));
        }

        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<UserListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(UserListItemModel.Organisation)).Visible = IsSysOrAppAdmin;
            return Json(definition);
        }

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> View(int id, string message, string bc = null, string ba = null, string bi = null)
        {
            var user = await GetUserToHandle(id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.View)).Succeeded)
            {
                IEnumerable<CustomerUnitUser> customerUnitsUsers = null;
                if (user.CustomerOrganisationId.HasValue)
                {
                    customerUnitsUsers = await _dbContext.CustomerUnitUsers.GetCustomerUnitsWithCustomerUnitForUser(user.Id).ToListAsync();
                }
                var model = new UserModel
                {
                    Message = message,
                    Id = id,
                    AllowDefaultSettings = user.CustomerOrganisationId.HasValue,
                    SendNewInvite = !user.EmailConfirmed,
                    UserName = user.UserName,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    Email = user.Email,
                    PhoneWork = user.PhoneNumber ?? "-",
                    PhoneCellphone = user.PhoneNumberCellphone ?? "-",
                    IsOrganisationAdministrator = user.Roles.Any(r => r.RoleId == CentralAdministratorRoleId),
                    IsCentralOrderHandler = user.Roles.Any(r => r.RoleId == CentralOrderHandlerRoleId),
                    DisplayCentralOrderHandler = user.CustomerOrganisationId.HasValue,
                    DisplayCentralAdmin = user.CustomerOrganisationId.HasValue || user.BrokerId.HasValue,
                    DisplayForAdminUser = UseRolesForAdminUser(user),
                    LastLoginAt = "{0:yyyy-MM-dd}".FormatSwedish(user.LastLoginAt) ?? "-",
                    Organisation = user.CustomerOrganisation?.Name ?? user.Broker?.Name ?? "-",
                    IsActive = user.IsActive,
                    IsApplicationAdministrator = user.Roles.Any(r => r.RoleId == ApplicationAdministratorRoleId),
                    IsImpersonator = user.Roles.Any(r => r.RoleId == ImpersonatorRoleId),
                    IsSystemAdministrator = user.Roles.Any(r => r.RoleId == SystemAdministratorRoleId),
                    UnitUsers = customerUnitsUsers?.Select(cu => new UnitUserModel
                    {
                        IsActive = cu.CustomerUnit.IsActive,
                        Name = cu.CustomerUnit.Name,
                        IsLocalAdmin = cu.IsLocalAdmin
                    }).ToList(),
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

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> Edit(int id, string bc, string ba, string bi)
        {
            var user = await GetUserToHandle(id);
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
            if (unitUsers != null && HighestLevelLoggedInUserType == UserTypes.LocalAdministrator)
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
                    IsOrganisationAdministrator = user.Roles.Any(r => r.RoleId == CentralAdministratorRoleId),
                    IsCentralOrderHandler = user.Roles.Any(r => r.RoleId == CentralOrderHandlerRoleId),
                    IsApplicationAdministrator = user.Roles.Any(r => r.RoleId == ApplicationAdministratorRoleId),
                    IsImpersonator = user.Roles.Any(r => r.RoleId == ImpersonatorRoleId),
                    IsSystemAdministrator = user.Roles.Any(r => r.RoleId == SystemAdministratorRoleId),
                    DisplayCentralOrderHandler = user.CustomerOrganisationId.HasValue,
                    DisplayCentralAdmin = user.CustomerOrganisationId.HasValue || user.BrokerId.HasValue,
                    DisplayForAdminUser = UseRolesForAdminUser(user),
                    IsActive = user.IsActive,
                    Organisation = user.CustomerOrganisation?.Name ?? user.Broker?.Name ?? "-",
                    UserType = HighestLevelLoggedInUserType,
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
        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> Edit(UserModel model)
        {
            if (ModelState.IsValid)
            {
                bool serversideValid = true;
                if (string.IsNullOrWhiteSpace(model.NameFirst))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.NameFirst), $"Kan inte bara innehålla mellanslag");
                }
                if (string.IsNullOrWhiteSpace(model.NameFamily))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.NameFamily), $"Kan inte bara innehålla mellanslag");
                }
                if (serversideValid)
                {
                    var user = await GetUserToHandle(model.Id.Value);
                    if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                    {
                        await _userService.LogOnUpdateAsync(model.Id.Value, User.GetUserId(), User.TryGetImpersonatorId());
                        user.NameFirst = model.NameFirst.Trim();
                        user.NameFamily = model.NameFamily.Trim();
                        user.PhoneNumber = model.PhoneWork?.Trim();
                        user.PhoneNumberCellphone = model.PhoneCellphone?.Trim();
                        if (user.IsActive && !model.IsActive)
                        {
                            await _userManager.UpdateSecurityStampAsync(user);
                        }
                        user.IsActive = model.IsActive;
                        if (user.CustomerOrganisationId.HasValue || user.BrokerId.HasValue)
                        {
                            await UpdateOrganisationAdministratorRoleAsync(model, user);
                        }
                        if (user.CustomerOrganisationId.HasValue)
                        {
                            await UpdateCentralOrderHandlerRoleAsync(model, user);
                        }
                        if (UseRolesForAdminUser(user))
                        {
                            await UpdateRolesForAdminUserAsync(model, user);
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
            else
            {
                return View(model);
            }
        }


        [Authorize(Policies.SystemCentralLocalAdmin)]
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
                        UserIsConnected = (customerUnitId.HasValue && customerUnitId == cu.CustomerUnitId) || (customerUnits.Count() == 1 && HighestLevelLoggedInUserType == UserTypes.LocalAdministrator),
                        CustomerUnitId = cu.CustomerUnitId
                    }).OrderByDescending(items => items.UserIsConnected).ToList();
            }
            return View(new UserModel
            {
                UserType = HighestLevelLoggedInUserType,
                UnitUsers = unitUsers,
                HasSelectedOrganisation = hasSelectedCustomer,
                OrganisationIdentifier = hasSelectedCustomer ? $"{customerOrganisationId}_{OrganisationType.GovernmentBody}" : null,
                Organisation = customerName,
                DisplayCentralOrderHandler = !User.TryGetBrokerId().HasValue,
                DisplayCentralAdmin = User.TryGetBrokerId().HasValue || User.TryGetCustomerOrganisationId().HasValue,
                HasSelectedCustomerunit = customerUnitId.HasValue,
                UserPageMode = new UserPageMode
                {
                    BackController = bc,
                    BackAction = ba,
                    BackId = bi
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                bool serversideValid = true;
                if (string.IsNullOrWhiteSpace(model.NameFirst))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.NameFirst), $"Kan inte bara innehålla mellanslag");
                }
                if (string.IsNullOrWhiteSpace(model.NameFamily))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.NameFamily), $"Kan inte bara innehålla mellanslag");
                }
                if (!_userService.IsUniqueEmail(model.Email))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Denna e-postadress används redan i tjänsten");
                }
                else if (HighestLevelLoggedInUserType == UserTypes.LocalAdministrator && !model.UnitUsers.Where(uu => uu.UserIsConnected).Any())
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Du måste koppla användaren till minst en enhet");
                }
                else if (IsSysOrAppAdmin)
                {
                    if (!string.IsNullOrWhiteSpace(model.OrganisationIdentifier) && Enum.Parse<OrganisationType>(model.OrganisationIdentifier.Split("_").Last()) == OrganisationType.Owner
                    && !model.IsApplicationAdministrator && !model.IsSystemAdministrator && !model.IsImpersonator)
                    {
                        serversideValid = false;
                        ModelState.AddModelError(nameof(model.IsImpersonator), $" Du måste koppla användaren till minst en roll");
                    }
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
                    model.UserType = HighestLevelLoggedInUserType;
                    model.DisplayCentralOrderHandler = !User.TryGetBrokerId().HasValue;
                    model.DisplayCentralAdmin = User.TryGetBrokerId().HasValue || User.TryGetCustomerOrganisationId().HasValue;
                }
                else
                {
                    using var trn = await _dbContext.Database.BeginTransactionAsync();
                    var additionalRoles = new List<string>();
                    var organisationPrefix = string.Empty;
                    int? customerId = null;
                    int? brokerId = null;
                    if (!IsSysOrAppAdmin)
                    {
                        customerId = User.TryGetCustomerOrganisationId();
                        brokerId = User.TryGetBrokerId();
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(model.OrganisationIdentifier))
                        {
                            var org = model.OrganisationIdentifier.Split("_");
                            var id = org.First().ToSwedishInt();
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
                        organisationPrefix = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == customerId).OrganisationPrefix;
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
                        _userService.GenerateUserName(model.NameFirst.Trim(), model.NameFamily.Trim(), organisationPrefix),
                        model.NameFirst.Trim(),
                        model.NameFamily.Trim())
                    {
                        CustomerOrganisationId = customerId,
                        BrokerId = brokerId,
                        CustomerUnits = unitUsers.Any() ? unitUsers : null
                    };
                    if (model.IsOrganisationAdministrator && (brokerId.HasValue || customerId.HasValue))
                    {
                        additionalRoles.Add(Roles.CentralAdministrator);
                    }
                    if (model.IsCentralOrderHandler && customerId.HasValue)
                    {
                        additionalRoles.Add(Roles.CentralOrderHandler);
                    }
                    //roles that only sysadmin or appadmin can add
                    if (IsSysOrAppAdmin && model.IsSystemAdministrator)
                    {
                        additionalRoles.Add(Roles.SystemAdministrator);
                    }
                    if (IsSysOrAppAdmin && model.IsApplicationAdministrator)
                    {
                        additionalRoles.Add(Roles.ApplicationAdministrator);
                    }
                    if (IsSysOrAppAdmin && model.IsImpersonator)
                    {
                        additionalRoles.Add(Roles.Impersonator);
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
                        await _userService.LogCreateAsync(user.Id, User.GetUserId(), User.TryGetImpersonatorId());

                        trn.Commit();
                        return RedirectToAction(model.UserPageMode.BackAction, model.UserPageMode.BackController, new { id = model.UserPageMode.BackId });
                    }
                    model.ErrorMessage = GetErrors(result);
                }
            }
            return View(model);
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> ViewOrganisationSettings(string message)
        {
            AspNetUser apiUser = await GetApiUser();

            if (apiUser != null)
            {
                var claims = await _dbContext.UserClaims.GetClaimsForUser(apiUser.Id);
                var notificationSettings = _dbContext.UserNotificationSettings.GetNotificationSettingsForUser(apiUser.Id);
                return View(new OrganisationSettingsModel
                {
                    Message = message,
                    ApiUserName = apiUser.UserName,
                    EmailRequests = apiUser.Email,
                    CertificateSerialNumber = claims.SingleOrDefault(c => c.ClaimType == "CertificateSerialNumber")?.ClaimValue,
                    UseApiKeyAuthentication = claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"),
                    UseCertificateAuthentication = claims.Any(c => c.ClaimType == "UseCertificateAuthentication"),
                    CallbackApiKey = claims.Any(c => c.ClaimType == "CallbackApiKey") ? EncryptHelper.Decrypt(claims.Single(c => c.ClaimType == "CallbackApiKey").ClaimValue, _options.PublicOrigin, apiUser.UserName) : null,
                    OrganisationNumber = apiUser.Broker?.OrganizationNumber,
                    ContactPhone = apiUser.Broker?.ContactPhoneNumber,
                    ContactEmail = apiUser.Broker?.ContactEmailAddress,
                    BrokerName = apiUser.Broker?.Name,
                    NotificationSettings = notificationSettings.Select(s => new NotificationSettingsDetailsModel
                    {
                        Type = s.NotificationType,
                        Channel = s.NotificationChannel,
                        ContactInformation = s.ConnectionInformation
                    })
                });
            }

            return Forbid();
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> EditNotificationSettings()
        {
            AspNetUser apiUser = await GetApiUser();
            if (apiUser != null)
            {
                return View(GetAvailableNotifications(_dbContext.UserNotificationSettings.GetNotificationSettingsForUser(apiUser.Id)).ToList());
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> EditNotificationSettings(IEnumerable<NotificationSettingsModel> model)
        {
            AspNetUser apiUser = await GetApiUser();
            if (apiUser != null)
            {
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                apiUser.NotificationSettings = await _dbContext.UserNotificationSettings.GetNotificationSettingsForUser(apiUser.Id).ToListAsync();
                if (ModelState.IsValid)
                {
                    await _userService.LogNotificationSettingsUpdateAsync(apiUser.Id, User.GetUserId(), User.TryGetImpersonatorId());
                    foreach (var setting in model)
                    {
                        UpdateNotificationSetting(apiUser, setting.UseEmail, setting.Type, NotificationChannel.Email, setting.SpecificEmail);
                        UpdateNotificationSetting(apiUser, setting.UseWebHook, setting.Type, NotificationChannel.Webhook, setting.WebHookReceipentAddress);
                    }
                    await _dbContext.SaveChangesAsync();
                    transaction.Complete();
                    await _cacheService.Flush(CacheKeys.OrganisationSettings);
                    return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                }
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> ChangeApiKey()
        {
            if (await GetApiUser() != null)
            {
                return View();
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
                AspNetUser apiUser = await GetApiUser();
                if (apiUser != null)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                    {
                        ModelState.AddModelError(nameof(model.CurrentPassword), "Lösenordet som angivits är felaktigt.");
                        return View(model);
                    }
                    using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
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

                    var salt = HashHelper.CreateSalt(32);
                    await _userManager.AddClaimAsync(apiUser, new Claim("Secret", HashHelper.GenerateHash(model.ApiKey, salt)));
                    await _userManager.AddClaimAsync(apiUser, new Claim("Salt", salt));
                    transaction.Complete();
                    return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> DisconnectUser(string combinedId)
        {
            var unitUser = await GetUnitUser(combinedId);
            var user = await GetUserToHandle(unitUser.UserId);
            bool removeOneSelfAsLocalAdmin = user.Id == User.GetUserId() && !User.IsInRole(Roles.CentralAdministrator);
            if ((await _authorizationService.AuthorizeAsync(User, unitUser, Policies.Edit)).Succeeded)
            {
                if (unitUser != null)
                {
                    if (IfLastUserWithLocalAdmin(unitUser))
                    {
                        return RedirectToAction("Users", "Unit", new { id = unitUser.CustomerUnitId, errorMessage = $"Det går inte att ta bort {user.FullName} som lokal administratör då enheten måste ha minst en användare som lokal administratör. Gör en annan användare till lokal administratör före borttag." });
                    }
                    await _userService.LogCustomerUnitUserUpdateAsync(unitUser.UserId, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.CustomerUnitUsers.Remove(unitUser);
                    _dbContext.SaveChanges();
                    return RedirectToAction("Users", "Unit", new
                    {
                        id = unitUser.CustomerUnitId,
                        message = removeOneSelfAsLocalAdmin ?
                        "Du är nu bortkopplad från enheten, dina rättigheter för enheten försvinner om fem minuter." :
                        $"{user.FullName} är bortkopplad från enheten"
                    });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.CentralLocalAdminCustomer)]
        public async Task<ActionResult> ChangeLocalAdmin(string combinedId)
        {
            var unitUser = await GetUnitUser(combinedId);
            var user = await GetUserToHandle(unitUser.UserId);
            bool removeOneSelfAsLocalAdmin = user.Id == User.GetUserId() && !User.IsInRole(Roles.CentralAdministrator) && unitUser.IsLocalAdmin;
            if ((await _authorizationService.AuthorizeAsync(User, unitUser, Policies.Edit)).Succeeded)
            {
                if (unitUser != null)
                {
                    if (IfLastUserWithLocalAdmin(unitUser))
                    {
                        return RedirectToAction("Users", "Unit", new { id = unitUser.CustomerUnitId, errorMessage = $"Det går inte att ta bort {user.FullName} som lokal administratör då enheten måste ha minst en användare som lokal administratör. Gör en annan användare till lokal administratör före borttag." });
                    }
                    await _userService.LogCustomerUnitUserUpdateAsync(unitUser.UserId, User.GetUserId(), User.TryGetImpersonatorId());
                    unitUser.IsLocalAdmin = !unitUser.IsLocalAdmin;
                    _dbContext.SaveChanges();
                }
                return RedirectToAction("Users", "Unit", new
                {
                    id = unitUser.CustomerUnitId,
                    message = removeOneSelfAsLocalAdmin ?
                    "Du är inte längre lokal administratör för enheten, dina administratörsrättigheter för enheten försvinner om fem minuter." :
                    $"Lokal administratör ändrad för {user.FullName}"
                });
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
            var user = await GetUserToHandle(model.ConnectUserId.Value);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Connect)).Succeeded)
            {
                await _userService.LogCustomerUnitUserUpdateAsync(model.ConnectUserId.Value, User.GetUserId(), User.TryGetImpersonatorId());
                _dbContext.CustomerUnitUsers.Add(new CustomerUnitUser { CustomerUnitId = model.CustomerUnitId, UserId = model.ConnectUserId.Value, IsLocalAdmin = model.IsLocalAdministrator });
                _dbContext.SaveChanges();
                return RedirectToAction("Users", "Unit", new { id = model.CustomerUnitId, message = $"{user.FullName} är nu kopplad till enheten" });
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        public async Task<ActionResult> EditOrganisationSettings()
        {
            AspNetUser apiUser = await GetApiUser();
            if (apiUser != null)
            {
                var claims = await _dbContext.UserClaims.GetClaimsForUser(apiUser.Id);
                return View(new OrganisationSettingsModel
                {
                    ApiUserName = apiUser.UserName,
                    EmailRequests = apiUser.Email,
                    CertificateSerialNumber = claims.SingleOrDefault(c => c.ClaimType == "CertificateSerialNumber")?.ClaimValue,
                    UseApiKeyAuthentication = claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"),
                    UseCertificateAuthentication = claims.Any(c => c.ClaimType == "UseCertificateAuthentication"),
                    CallbackApiKey = claims.Any(c => c.ClaimType == "CallbackApiKey") ? EncryptHelper.Decrypt(claims.SingleOrDefault(c => c.ClaimType == "CallbackApiKey").ClaimValue, _options.PublicOrigin, apiUser.UserName) : null,
                    OrganisationNumber = apiUser.Broker?.OrganizationNumber,
                    ContactPhone = apiUser.Broker?.ContactPhoneNumber,
                    ContactEmail = apiUser.Broker?.ContactEmailAddress,
                    BrokerName = apiUser.Broker?.Name,
                });
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
                AspNetUser apiUser = await GetApiUser();
                if (apiUser != null)
                {
                    using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                    await _userService.LogOnUpdateAsync(apiUser.Id, User.GetUserId(), User.TryGetImpersonatorId());
                    if (apiUser.BrokerId.HasValue)
                    {
                        if (apiUser.NormalizedEmail != model.EmailRequests.ToSwedishUpper())
                        {
                            apiUser.Email = model.EmailRequests;
                            apiUser.NormalizedEmail = model.EmailRequests.ToSwedishUpper();
                            apiUser.Broker.EmailAddress = model.EmailRequests;
                        }
                        apiUser.Broker.OrganizationNumber = model.OrganisationNumber;
                        apiUser.Broker.ContactEmailAddress = model.ContactEmail;
                        apiUser.Broker.ContactPhoneNumber = model.ContactPhone;
                    }
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
                    claim = claims.SingleOrDefault(c => c.Type == nameof(model.CallbackApiKey));
                    if (claim != null)
                    {
                        await _userManager.RemoveClaimAsync(brokerUser, claim);
                    }
                    if (!string.IsNullOrWhiteSpace(model.CallbackApiKey))
                    {
                        await _userManager.AddClaimAsync(brokerUser, new Claim(nameof(model.CallbackApiKey), EncryptHelper.Encrypt(model.CallbackApiKey, _options.PublicOrigin, apiUser.UserName)));
                    }
                    await _dbContext.SaveChangesAsync();
                    transaction.Complete();
                    return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                }
            }
            return View(model);
        }

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> ViewDefaultSettings(int id, string bi = null, string bc = null, string ba = null)
        {
            var user = await _userService.GetUserWithDefaultSettings(id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.ViewDefaultSettings)).Succeeded)
            {
                var model = DefaultSettingsViewModel.GetModel(user, Region.Regions);
                model.Id = user.Id;
                model.UserPageMode = new UserPageMode
                {
                    BackController = bc ?? BackController,
                    BackAction = ba ?? BackAction,
                    BackId = bi ?? BackId
                };
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> ChangeEmail(int id, string bc, string ba, string bi)
        {
            var user = await GetUserToHandle(id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
            {
                var model = new UserModel
                {
                    Id = user.Id,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    Email = user.Email,
                    SendNewInvite = !user.EmailConfirmed,
                    IsEditOrCreate = false,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> ChangeEmail(UserModel model)
        {
            var user = await GetUserToHandle(model.Id.Value);
            if (ModelState.IsValid)
            {
                model.SendNewInvite = !user.EmailConfirmed;
                model.IsEditOrCreate = false;
                var messageToUser = string.Empty;
                if (user.Email.ToSwedishUpper() == model.Email.ToSwedishUpper())
                {
                    ModelState.AddModelError(nameof(model.Email), "Du har inte ändrat på e-postadressen");
                    return View(model);
                }
                else if (!_userService.IsUniqueEmail(model.Email, user.Id))
                {
                    ModelState.AddModelError(nameof(model.Email), "Denna e-postadress används redan i tjänsten");
                    return View(model);
                }

                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                {
                    if (!user.EmailConfirmed)
                    {
                        await _userService.LogUpdateEmailAsync(model.Id.Value, User.GetUserId(), User.TryGetImpersonatorId());
                        user.Email = model.Email;
                        user.NormalizedEmail = model.Email.ToSwedishUpper();
                        //activate a user that might be inactive due to job that inactivates?
                        user.IsActive = true;
                        await _userManager.UpdateAsync(user);
                        //send new invite
                        await _userService.SendInviteAsync(user);
                        messageToUser = "E-postadressen är ändrad och ny aktiveringslänk är skickad till användaren";
                    }
                    else
                    {
                        await _userService.SetTemporaryEmail(user, model.Email, User.GetUserId(), User.TryGetImpersonatorId());
                        var code = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                        await _userService.SendChangedEmailLink(user, model.Email, Url.ChangeEmailCallbackLink(user.Id.ToSwedishString(), code), true);
                        messageToUser = "För att slutföra ändringen, be användaren att följa instruktionerna i meddelandet som skickats till den nya e-postadressen";
                    }
                }
                return RedirectToAction(nameof(View), new { id = model.Id, bc = model.UserPageMode.BackController, ba = model.UserPageMode.BackAction, bi = model.UserPageMode.BackId, message = messageToUser });
            }
            return View(model);
        }



        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> SendNewInvite(int id, string bc, string ba, string bi)
        {
            var user = await GetUserToHandle(id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded && !user.EmailConfirmed)
            {
                var model = new UserModel
                {
                    Id = user.Id,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    IsEditOrCreate = false,
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
        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> SendNewInvite(UserModel model)
        {
            var user = await GetUserToHandle(model.Id.Value);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded && !user.EmailConfirmed)
                {
                    //activate a user that might be inactive due to job that inactivates?
                    user.IsActive = true;
                    await _userService.SendInviteAsync(user);
                }
                return RedirectToAction(nameof(View), new { id = model.Id, bc = model.UserPageMode.BackController, ba = model.UserPageMode.BackAction, bi = model.UserPageMode.BackId, message = "En ny inbjudan med aktiveringslänk är skickad till användaren" });
            }
            return View(model);
        }

        private async Task<AspNetUser> GetApiUser()
        {
            var brokerId = User.TryGetBrokerId();
            var customerId = User.TryGetCustomerOrganisationId();
            if (brokerId != null)
            {
                return await _dbContext.Users.GetAPIUserForBroker(brokerId.Value);
            }
            else if (customerId != null)
            {
                return await _dbContext.Users.GetAPIUserForCustomer(customerId.Value);
            }

            return null;
        }

        private bool UseRolesForAdminUser(AspNetUser user)
        {
            return !user.CustomerOrganisationId.HasValue && !user.BrokerId.HasValue && !user.InterpreterId.HasValue && IsSysOrAppAdmin;
        }

        private bool IsSysOrAppAdmin => User.IsInRole(Roles.ApplicationAdministrator) || User.IsInRole(Roles.SystemAdministrator);

        private string BackController => HttpContext.Request.Headers["Referer"].ToString().ContainsSwedish("Customer") ? "Customer" : HttpContext.Request.Headers["Referer"].ToString().ContainsSwedish("Unit") ? "Unit" : "User";

        private string BackAction => BackController == "User" ? "List" : BackController == "Unit" ? "Users" : "View";

        private string BackId
        {
            get
            {
                var referer = HttpContext.Request.Headers["Referer"].ToString();
                if (!(referer.ContainsSwedish("Unit") || referer.ContainsSwedish("Customer")))
                {
                    return string.Empty;
                }
                var id = referer.Split("/").Last();
                return id.Contains("?", StringComparison.OrdinalIgnoreCase) ? id.Split("?").First() : id;
            }
        }

        private async Task UpdateCentralOrderHandlerRoleAsync(UserModel model, AspNetUser user)
        {
            if (model.IsCentralOrderHandler && !user.Roles.Any(r => r.RoleId == CentralOrderHandlerRoleId))
            {
                await _userManager.AddToRoleAsync(user, Roles.CentralOrderHandler);
            }
            else if (!model.IsCentralOrderHandler && user.Roles.Any(r => r.RoleId == CentralOrderHandlerRoleId))
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.CentralOrderHandler);
            }
        }

        private async Task UpdateOrganisationAdministratorRoleAsync(UserModel model, AspNetUser user)
        {
            if (model.IsOrganisationAdministrator && !user.Roles.Any(r => r.RoleId == CentralAdministratorRoleId))
            {
                await _userManager.AddToRoleAsync(user, Roles.CentralAdministrator);
            }
            else if (!model.IsOrganisationAdministrator && user.Roles.Any(r => r.RoleId == CentralAdministratorRoleId))
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.CentralAdministrator);
            }
        }

        private async Task UpdateRolesForAdminUserAsync(UserModel model, AspNetUser user)
        {
            if (model.IsApplicationAdministrator && !user.Roles.Any(r => r.RoleId == ApplicationAdministratorRoleId))
            {
                await _userManager.AddToRoleAsync(user, Roles.ApplicationAdministrator);
            }
            else if (!model.IsApplicationAdministrator && user.Roles.Any(r => r.RoleId == ApplicationAdministratorRoleId))
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.ApplicationAdministrator);
            }
            if (model.IsSystemAdministrator && !user.Roles.Any(r => r.RoleId == SystemAdministratorRoleId))
            {
                await _userManager.AddToRoleAsync(user, Roles.SystemAdministrator);
            }
            else if (!model.IsSystemAdministrator && user.Roles.Any(r => r.RoleId == SystemAdministratorRoleId))
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.SystemAdministrator);
            }
            if (model.IsImpersonator && !user.Roles.Any(r => r.RoleId == ImpersonatorRoleId))
            {
                await _userManager.AddToRoleAsync(user, Roles.Impersonator);
            }
            else if (!model.IsImpersonator && user.Roles.Any(r => r.RoleId == ImpersonatorRoleId))
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.Impersonator);
            }
        }

        private UserTypes HighestLevelLoggedInUserType =>
            User.IsInRole(Roles.ApplicationAdministrator) ? UserTypes.ApplicationAdministrator :
            User.IsInRole(Roles.SystemAdministrator) ? UserTypes.SystemAdministrator :
            User.IsInRole(Roles.CentralAdministrator) ? UserTypes.OrganisationAdministrator : UserTypes.LocalAdministrator;

        private IEnumerable<CustomerUnit> LoggedInCustomerUnits => User.TryGetCustomerOrganisationId().HasValue ?
            User.IsInRole(Roles.CentralAdministrator) ?
            _dbContext.CustomerUnits.Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId()) :
            _dbContext.CustomerUnits.Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId()
            && cu.CustomerUnitUsers.Any(cuu => cuu.UserId == User.GetUserId() && cuu.IsLocalAdmin)) : null;

        private async Task<AspNetUser> GetUserToHandle(int userId)
        {
            var user = await _userManager.Users
                .Include(u => u.CustomerOrganisation)
                .Include(u => u.Broker)
                .SingleOrDefaultAsync(u => u.Id == userId);
            user.CustomerUnits = await _dbContext.CustomerUnitUsers.GetCustomerUnitsForUser(userId).ToListAsync();
            user.Roles = await _dbContext.UserRoles.GetRolesForUser(userId).ToListAsync();

            return user;
        }

        private async Task<CustomerUnitUser> GetUnitUser(string combinedId)
        {
            int userId = combinedId.Split("_")[0].ToSwedishInt();
            int customerUnitId = combinedId.Split("_")[1].ToSwedishInt();
            return await _dbContext.CustomerUnitUsers.GetCustomerUnitUserForUserAndCustomerUnit(userId, customerUnitId);
        }

        private bool IfLastUserWithLocalAdmin(CustomerUnitUser unitUser)
        {
            return unitUser.IsLocalAdmin && !_dbContext.CustomerUnitUsers.Where(cu => cu.CustomerUnitId == unitUser.CustomerUnitId && cu.UserId != unitUser.UserId && cu.IsLocalAdmin).Any();
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

        private static IEnumerable<NotificationSettingsModel> GetAvailableNotifications(IEnumerable<UserNotificationSetting> settings)
        {
            foreach (var val in EnumHelper.GetAllFullDescriptions<NotificationType>(null, false).Where(t => t.Value.GetAvailableNotificationConsumerTypes().Any(c => c == NotificationConsumerType.Broker)))
            {
                var emailSettings = settings.SingleOrDefault(s => s.NotificationType == val.Value && s.NotificationChannel == NotificationChannel.Email);
                var webhookSettings = settings.SingleOrDefault(s => s.NotificationType == val.Value && s.NotificationChannel == NotificationChannel.Webhook);
                var availableNotificationChannels = val.Value.GetAvailableNotificationChannels();
                yield return new NotificationSettingsModel
                {
                    Type = val.Value,
                    UseEmail = emailSettings != null,
                    SpecificEmail = emailSettings?.ConnectionInformation,
                    UseWebHook = webhookSettings != null,
                    WebHookReceipentAddress = webhookSettings?.ConnectionInformation,
                    DisplayEmail = availableNotificationChannels.Any(nc => nc == NotificationChannel.Email),
                    DisplayWebhook = availableNotificationChannels.Any(nc => nc == NotificationChannel.Webhook)
                };
            }
        }

        private static string GetErrors(IdentityResult result)
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
