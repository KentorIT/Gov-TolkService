using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using Tolk.BusinessLogic;
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
        private readonly ILogger _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserService _userService;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationService _notificationService;
        private readonly HashService _hashService;
        private readonly TolkOptions _options;

        public UserController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            ILogger<UserController> logger,
            RoleManager<IdentityRole<int>> roleManager,
            UserService userService,
            IAuthorizationService authorizationService,
            INotificationService notificationService,
            HashService hashService,
            IOptions<TolkOptions> options
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
            _options = options.Value;
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
            else if (model.UserType == UserType.SystemAdministrator)
            {
                users = users.Where(u => !u.Roles.Any(r => r.RoleId == ApplicationAdministratorRoleId || r.RoleId == ImpersonatorRoleId));
            }
            else if (model.UserType != UserType.ApplicationAdministrator)
            {
                return Forbid();
            }
            return AjaxDataTableHelper.GetData(request, users.Count(), model.Apply(users, _roleManager.Roles.Select(r => new RoleMap { Id = r.Id, Name = r.Name })), x => x.Select(u => new UserListItemModel
            {
                UserId = u.Id,
                Email = u.Email,
                Name = $"{u.NameFamily}, {u.NameFirst}",
                Organisation = u.CustomerOrganisation.Name ?? u.Broker.Name ?? "-",
                LastLoginAt = string.Format("{0:yyyy-MM-dd}", u.LastLoginAt) ?? "-",
                IsActive = u.IsActive
            }));
        }

        public JsonResult ListColumnDefinition()
        {
            var userType = HighestLevelLoggedInUserType;
            var definition = AjaxDataTableHelper.GetColumnDefinitions<UserListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(UserListItemModel.Organisation)).Visible = (userType == UserType.ApplicationAdministrator || userType == UserType.SystemAdministrator);
            return Json(definition);
        }

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> View(int id, string message, string bc = null, string ba = null, string bi = null)
        {
            var user = GetUserToHandle(id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.View)).Succeeded &&
                !(HighestLevelLoggedInUserType == UserType.SystemAdministrator && user.Roles.Any(r => r.RoleId == ImpersonatorRoleId && r.RoleId == ApplicationAdministratorRoleId)))
            {
                IEnumerable<CustomerUnit> customerUnits = null;
                if (user.CustomerOrganisationId.HasValue)
                {
                    customerUnits = _dbContext.CustomerUnits
                        .Include(cu => cu.CustomerUnitUsers)
                        .Where(cu => cu.CustomerOrganisationId == user.CustomerOrganisationId
                        && cu.CustomerUnitUsers.Any(cuu => cuu.UserId == user.Id)).OrderByDescending(cu => cu.IsActive).ThenBy(cu => cu.Name);
                }
                var model = new UserModel
                {
                    Message = message,
                    Id = id,
                    AllowDefaultSettings = _options.EnableDefaultSettings && user.CustomerOrganisationId.HasValue,
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
                    LastLoginAt = string.Format("{0:yyyy-MM-dd}", user.LastLoginAt) ?? "-",
                    Organisation = user.CustomerOrganisation?.Name ?? user.Broker?.Name ?? "-",
                    IsActive = user.IsActive,
                    IsApplicationAdministrator = user.Roles.Any(r => r.RoleId == ApplicationAdministratorRoleId),
                    IsImpersonator = user.Roles.Any(r => r.RoleId == ImpersonatorRoleId),
                    IsSystemAdministrator = user.Roles.Any(r => r.RoleId == SystemAdministratorRoleId),
                    UnitUsers = customerUnits?.Select(cu => new UnitUserModel
                    {
                        IsActive = cu.IsActive,
                        Name = cu.Name,
                        IsLocalAdmin = cu.CustomerUnitUsers.SingleOrDefault(cuu => cuu.UserId == user.Id).IsLocalAdmin
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
            var user = _userManager.Users.Include(u => u.Roles)
                .Include(u => u.CustomerUnits).ThenInclude(cu => cu.CustomerUnit)
                .Include(u => u.CustomerOrganisation)
                .Include(u => u.Broker)
                .SingleOrDefault(u => u.Id == id);
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
            if (unitUsers != null && HighestLevelLoggedInUserType == UserType.LocalAdministrator)
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

            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded &&
                 !(HighestLevelLoggedInUserType == UserType.SystemAdministrator && user.Roles.Any(r => r.RoleId == ImpersonatorRoleId && r.RoleId == ApplicationAdministratorRoleId)))
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
                var user = _userManager.Users.Include(u => u.Roles).Include(u => u.CustomerUnits).SingleOrDefault(u => u.Id == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded &&
                     !(HighestLevelLoggedInUserType == UserType.SystemAdministrator && user.Roles.Any(r => r.RoleId == ImpersonatorRoleId && r.RoleId == ApplicationAdministratorRoleId)))
                {
                    await _userService.LogOnUpdateAsync(model.Id.Value, User.GetUserId());
                    user.NameFirst = model.NameFirst;
                    user.NameFamily = model.NameFamily;
                    user.PhoneNumber = model.PhoneWork;
                    user.PhoneNumberCellphone = model.PhoneCellphone;
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
                        UserIsConnected = (customerUnitId.HasValue && customerUnitId == cu.CustomerUnitId) ? true : (customerUnits.Count() == 1 && HighestLevelLoggedInUserType == UserType.LocalAdministrator) ? true : false,
                        CustomerUnitId = cu.CustomerUnitId
                    }).OrderByDescending(items => items.UserIsConnected).ToList();
            }
            return View(new UserModel
            {
                UserType = HighestLevelLoggedInUserType,
                UnitUsers = unitUsers,
                HasSelectedOrganisation = hasSelectedCustomer,
                OrganisationIdentifier = hasSelectedCustomer ? $"{customerOrganisationId.ToString()}_{OrganisationType.GovernmentBody}" : null,
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
                if (!_userService.IsUniqueEmail(model.Email))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Denna e-postadress används redan i tjänsten");
                }
                else if (HighestLevelLoggedInUserType == UserType.LocalAdministrator && !model.UnitUsers.Where(uu => uu.UserIsConnected).Any())
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Du måste koppla användaren till minst en enhet");
                }
                else if (HighestLevelLoggedInUserType == UserType.ApplicationAdministrator)
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
                    using (var trn = await _dbContext.Database.BeginTransactionAsync())
                    {
                        var additionalRoles = new List<string>();
                        var organisationPrefix = string.Empty;
                        int? customerId = null;
                        int? brokerId = null;
                        if (!User.IsInRole(Roles.ApplicationAdministrator) && !User.IsInRole(Roles.SystemAdministrator))
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
                            _userService.GenerateUserName(model.NameFirst, model.NameFamily, organisationPrefix),
                            model.NameFirst,
                            model.NameFamily)
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
                        //if no role is selected and KamK user = (SysAdmin creator or if AppAdmin does not sekect any)
                        if ((!brokerId.HasValue && !customerId.HasValue && !model.IsApplicationAdministrator && !model.IsImpersonator) || model.IsSystemAdministrator)
                        {
                            additionalRoles.Add(Roles.SystemAdministrator);
                        }
                        if (HighestLevelLoggedInUserType == UserType.ApplicationAdministrator && model.IsApplicationAdministrator)
                        {
                            additionalRoles.Add(Roles.ApplicationAdministrator);
                        }
                        if (HighestLevelLoggedInUserType == UserType.ApplicationAdministrator && model.IsImpersonator)
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
                        CallbackApiKey = apiUser.Claims.Any(c => c.ClaimType == "CallbackApiKey") ? EncryptHelper.Decrypt(apiUser.Claims.Single(c => c.ClaimType == "CallbackApiKey").ClaimValue, _options.PublicOrigin, apiUser.UserName) : null,
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
                            await _userService.LogNotificationSettingsUpdateAsync(apiUser.Id, User.GetUserId());
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
                        CallbackApiKey = apiUser.Claims.Any(c => c.ClaimType == "CallbackApiKey") ? EncryptHelper.Decrypt(apiUser.Claims.SingleOrDefault(c => c.ClaimType == "CallbackApiKey").ClaimValue, _options.PublicOrigin, apiUser.UserName) : null,
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
                }
            }
            return View(model);
        }

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> ViewDefaultSettings(int? id = null, string bc = null, string ba = null, string message = null)
        {
            //Flytta allt detta kod till userService, och flytta action till account.
            //    Sedan kan vi ha kvar den här, men den tar inte en nullable int!
            //Sedan tror jag att man skall ha en egen policy för detta EditDefaultSettings som man kan skicka in user och unit till...?
            if (!id.HasValue)
            {
                id = User.GetUserId();
            }
            var user = await _userManager.Users
                .Include(u => u.DefaultSettings)
                .Include(u => u.CustomerUnits).ThenInclude(c => c.CustomerUnit)
                .SingleOrDefaultAsync(u => u.Id == id);
            if (_options.EnableDefaultSettings && (await _authorizationService.AuthorizeAsync(User, user, Policies.ViewDefaultSettings)).Succeeded)
            {
                int? customerUnit = user.GetIntValue(DefaultSettingsType.CustomerUnit);
                var model = new DefaultSettingsViewModel
                {
                    Id = id.Value,
                    Message = message,
                    ShowUnitSelection = user.CustomerUnits.Any(),
                    AllowChange = id == User.GetUserId(),
                    Region = Region.Regions.SingleOrDefault(r => r.RegionId == user.GetIntValue(DefaultSettingsType.Region))?.Name,
                    CustomerUnit = customerUnit == 0 ? Constants.SelectNoUnit : user.CustomerUnits.SingleOrDefault(c => c.CustomerUnitId == customerUnit)?.CustomerUnit.Name,
                    RankedInterpreterLocationFirst = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationPrimary),
                    RankedInterpreterLocationSecond = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationSecondary),
                    RankedInterpreterLocationThird = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationThird),
                    OnSiteLocationStreet = user.GetValue(DefaultSettingsType.OnSiteStreet),
                    OnSiteLocationCity = user.GetValue(DefaultSettingsType.OnSiteCity),
                    OffSiteDesignatedLocationStreet = user.GetValue(DefaultSettingsType.OffSiteDesignatedLocationStreet),
                    OffSiteDesignatedLocationCity = user.GetValue(DefaultSettingsType.OffSiteDesignatedLocationCity),
                    OffSitePhoneContactInformation = user.GetValue(DefaultSettingsType.OffSitePhoneContactInformation),
                    OffSiteVideoContactInformation = user.GetValue(DefaultSettingsType.OffSiteVideoContactInformation),
                    AllowExceedingTravelCost = user.TryGetEnumValue<AllowExceedingTravelCost>(DefaultSettingsType.AllowExceedingTravelCost),
                    InvoiceReference = user.GetValue(DefaultSettingsType.InvoiceReference),
                    UserPageMode = new UserPageMode
                    {
                        BackController = bc ?? BackController,
                        BackAction = ba ?? BackAction,
                        BackId = id?.ToString() ?? BackId
                    }
                };
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<ActionResult> EditDefaultSettings(int? id = null, string bc = null, string ba = null)
        {
            if (_options.EnableDefaultSettings)
            {
                //Flytta allt detta kod till userService, och flytta action till account.
                //    Sedan kan vi ha kvar den här, men den tar inte en nullable int!
                //Sedan tror jag att man skall ha en egen policy för detta EditDefaultSettings som man kan skicka in user och unit till...?
                if (!id.HasValue)
                {
                    id = User.GetUserId();
                }
                var user = await _userManager.Users
                    .Include(u => u.DefaultSettings)
                    .SingleOrDefaultAsync(u => u.Id == id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.EditDefaultSettings)).Succeeded)
                {
                    AllowExceedingTravelCost? cost = user.TryGetEnumValue<AllowExceedingTravelCost>(DefaultSettingsType.AllowExceedingTravelCost);
                    var model = new DefaultSettingsModel
                    {
                        Id = id.Value,
                        //Make attribute that takes a DefaultSettingsType, gets the type from the property and parses in a switch, get the value from an extension on aspnetuser?
                        //then make a twin attribute that is set on order model properties, to make them connected to the correct default value.
                        //This should not be set in controller though, but sent as array to client to be set on load.
                        // if done this way, the units can have their own set of default sent in the same way.
                        RegionId = user.GetIntValue(DefaultSettingsType.Region),
                        CustomerUnitId = user.GetIntValue(DefaultSettingsType.CustomerUnit),
                        RankedInterpreterLocationFirst = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationPrimary),
                        RankedInterpreterLocationSecond = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationSecondary),
                        RankedInterpreterLocationThird = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationThird),
                        OnSiteLocationStreet = user.GetValue(DefaultSettingsType.OnSiteStreet),
                        OnSiteLocationCity = user.GetValue(DefaultSettingsType.OnSiteCity),
                        OffSiteDesignatedLocationStreet = user.GetValue(DefaultSettingsType.OffSiteDesignatedLocationStreet),
                        OffSiteDesignatedLocationCity = user.GetValue(DefaultSettingsType.OffSiteDesignatedLocationCity),
                        OffSitePhoneContactInformation = user.GetValue(DefaultSettingsType.OffSitePhoneContactInformation),
                        OffSiteVideoContactInformation = user.GetValue(DefaultSettingsType.OffSiteVideoContactInformation),
                        AllowExceedingTravelCost = user.TryGetEnumValue<AllowExceedingTravelCost>(DefaultSettingsType.AllowExceedingTravelCost),
                        InvoiceReference = user.GetValue(DefaultSettingsType.InvoiceReference),
                        UserPageMode = new UserPageMode
                        {
                            BackController = bc ?? BackController,
                            BackAction = ba ?? BackAction,
                            BackId = id?.ToString() ?? BackId
                        }
                    };
                    return View(model);
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<ActionResult> EditDefaultSettings(DefaultSettingsModel model)
        {
            if (!_options.EnableDefaultSettings)
            {
                return Forbid();
            }
            if (ModelState.IsValid)
            {
                var user = await _userManager.Users
                    .Include(u => u.DefaultSettings)
                    .SingleOrDefaultAsync(u => u.Id == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.EditDefaultSettings)).Succeeded)
                {
                    await _userService.LogDefaultSettingsUpdateAsync(model.Id, User.GetUserId());
                    UpdateDefaultSetting(user, DefaultSettingsType.Region, model.RegionId?.ToString());
                    UpdateDefaultSetting(user, DefaultSettingsType.CustomerUnit, model.CustomerUnitId?.ToString());

                    //InterpreterLocations
                    UpdateDefaultSetting(user, DefaultSettingsType.InterpreterLocationPrimary, ((int?)model.RankedInterpreterLocationFirst)?.ToString());
                    UpdateDefaultSetting(user, DefaultSettingsType.InterpreterLocationSecondary, ((int?)model.RankedInterpreterLocationSecond)?.ToString());
                    UpdateDefaultSetting(user, DefaultSettingsType.InterpreterLocationThird, ((int?)model.RankedInterpreterLocationThird)?.ToString());

                    UpdateDefaultSetting(user, DefaultSettingsType.OnSiteStreet, model.OnSiteLocationStreet);
                    UpdateDefaultSetting(user, DefaultSettingsType.OnSiteCity, model.OnSiteLocationCity);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSiteDesignatedLocationStreet, model.OffSiteDesignatedLocationStreet);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSiteDesignatedLocationCity, model.OffSiteDesignatedLocationCity);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSitePhoneContactInformation, model.OffSitePhoneContactInformation);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSiteVideoContactInformation, model.OffSiteVideoContactInformation);
                    UpdateDefaultSetting(user, DefaultSettingsType.AllowExceedingTravelCost, ((int?)model.AllowExceedingTravelCost)?.ToString());
                    UpdateDefaultSetting(user, DefaultSettingsType.InvoiceReference, model.InvoiceReference);

                    await _dbContext.SaveChangesAsync();
                    //Need to update the loaded user's claims list after save.
                    // it is done here (GenerateClaimsAsync), but need to find out how it is initiated...

                    return RedirectToAction(nameof(ViewDefaultSettings), "User", new
                    {
                        message = "Ändringarna sparades",
                        ba = model.UserPageMode.BackAction,
                        bc = model.UserPageMode.BackController,
                        id = model.Id
                    });
                }
                return Forbid();
            }
            return View(model);
        }

        [Authorize(Policies.SystemCentralLocalAdmin)]
        public async Task<ActionResult> ChangeEmail(int id, string bc, string ba, string bi)
        {
            var user = _userManager.Users.Include(u => u.Roles).Include(u => u.CustomerUnits).SingleOrDefault(u => u.Id == id);
            if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
            {
                var model = new UserModel
                {
                    Id = user.Id,
                    NameFirst = user.NameFirst,
                    NameFamily = user.NameFamily,
                    Email = user.Email,
                    SendNewInvite = !user.EmailConfirmed,
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
        public async Task<ActionResult> ChangeEmail(UserModel model)
        {
            var user = _userManager.Users.Include(u => u.Roles).Include(u => u.CustomerUnits).SingleOrDefault(u => u.Id == model.Id);
            if (ModelState.IsValid)
            {
                var messageToUser = string.Empty;
                if (user.Email.ToUpper() == model.Email.ToUpper())
                {
                    ModelState.AddModelError(nameof(model.Email), "Du har inte ändrat på e-postadressen");
                    return View(model);
                }
                else if (!_userService.IsUniqueEmail(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "Denna e-postadress finns redan för en annan användare");
                    return View(model);
                }

                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                {
                    if (!user.EmailConfirmed)
                    {
                        await _userService.LogUpdateEmailAsync(model.Id.Value, User.GetUserId());
                        user.Email = model.Email;
                        user.NormalizedEmail = model.Email.ToUpper();
                        //activate a user that might be inactive due to job that inactivates?
                        user.IsActive = true;
                        await _userManager.UpdateAsync(user);
                        //send new invite
                        await _userService.SendInviteAsync(user);
                        messageToUser = "E-postadressen är ändrad och ny aktiveringslänk är skickad till användaren";
                    }
                    else
                    {
                        await _userService.SetTemporaryEmail(user, model.Email);
                        //await SendChangedEmailLink(user, model.Email);
                        var code = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                        await _userService.SendChangedEmailLink(user, model.Email, Url.ChangeEmailCallbackLink(user.Id.ToString(), code), true);
                        messageToUser = "För att slutföra ändringen, be användaren att följa instruktionerna i meddelandet som skickats till den nya e-postadressen";
                    }
                }
                //message that all is done!
                return RedirectToAction(nameof(View), new { id = model.Id, bc = model.UserPageMode.BackController, ba = model.UserPageMode.BackAction, bi = model.UserPageMode.BackId, message = messageToUser });
            }
            return View(model);
        }

        private bool UseRolesForAdminUser(AspNetUser user)
        {
            return !user.CustomerOrganisationId.HasValue && !user.BrokerId.HasValue && User.IsInRole(Roles.ApplicationAdministrator) && !user.InterpreterId.HasValue;
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

        private UserType HighestLevelLoggedInUserType =>
            User.IsInRole(Roles.ApplicationAdministrator) ? UserType.ApplicationAdministrator :
            User.IsInRole(Roles.SystemAdministrator) ? UserType.SystemAdministrator :
            User.IsInRole(Roles.CentralAdministrator) ? UserType.OrganisationAdministrator : UserType.LocalAdministrator;

        private IEnumerable<CustomerUnit> LoggedInCustomerUnits => User.TryGetCustomerOrganisationId().HasValue ?
            User.IsInRole(Roles.CentralAdministrator) ?
            _dbContext.CustomerUnits.Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId()) :
            _dbContext.CustomerUnits.Where(cu => cu.CustomerOrganisationId == User.TryGetCustomerOrganisationId()
            && cu.CustomerUnitUsers.Any(cuu => cuu.UserId == User.GetUserId() && cuu.IsLocalAdmin)) : null;

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

        private void UpdateDefaultSetting(AspNetUser user, DefaultSettingsType type, string value)
        {
            var setting = user.DefaultSettings.SingleOrDefault(s => s.DefaultSettingType == type);
            if (setting == null && !string.IsNullOrEmpty(value))
            {
                user.DefaultSettings.Add(new UserDefaultSetting
                {
                    DefaultSettingType = type,
                    Value = value,
                });
            }
            else if (setting != null && !string.IsNullOrEmpty(value))
            {
                setting.Value = value;
            }
            else if (setting != null && string.IsNullOrEmpty(value))
            {
                _dbContext.UserDefaultSettings.Remove(setting);
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
