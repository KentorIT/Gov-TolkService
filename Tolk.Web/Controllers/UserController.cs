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

            model.IsSystemAdministrator = User.IsInRole(Roles.SystemAdministrator);

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
            else if (!User.IsInRole(Roles.SystemAdministrator))
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
                    IsCentralAdministrator = user.Roles.Any(r => r.RoleId == centralAdministratorId),
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
            int centralAdministratorId = _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
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
                    IsCentralAdministrator = user.Roles.Any(r => r.RoleId == centralAdministratorId),
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
                int centralAdministratorId = _roleManager.Roles.Single(r => r.Name == Roles.CentralAdministrator).Id;
                var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, user, Policies.Edit)).Succeeded)
                {
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
                    if (model.IsCentralAdministrator && !user.Roles.Any(r => r.RoleId == centralAdministratorId))
                    {
                        await _userManager.AddToRoleAsync(user, Roles.CentralAdministrator);
                    }
                    else if (!model.IsCentralAdministrator && user.Roles.Any(r => r.RoleId == centralAdministratorId))
                    {
                        await _userManager.RemoveFromRoleAsync(user, Roles.CentralAdministrator);
                    }

                    await _userManager.UpdateAsync(user);
                }
            }
            return RedirectToAction(nameof(View), model);
        }

        public ActionResult Create()
        {
            return View(new UserModel { EditorIsSystemAdministrator = User.IsInRole(Roles.SystemAdministrator) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                if (!_userService.IsUniqueEmail(model.Email))
                {
                    model.EditorIsSystemAdministrator = User.IsInRole(Roles.SystemAdministrator);
                    ModelState.AddModelError(nameof(model.Email), $"Denna e-postadress används redan i tjänsten.");
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

                        var user = new AspNetUser(model.Email,
                            _userService.GenerateUserName(model.NameFirst, model.NameFamily, organisationPrefix),
                            model.NameFirst,
                            model.NameFamily)
                        {
                            CustomerOrganisationId = customerId,
                            BrokerId = brokerId,
                        };
                        if (model.IsCentralAdministrator)
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
                            return RedirectToAction(nameof(List), new UserFilterModel { Email = user.Email });
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
                            await _userManager.AddClaimAsync(apiUser, new Claim("Secret", _hashService.GenerateHash( model.ApiKey, salt)));
                            await _userManager.AddClaimAsync(apiUser, new Claim("Salt", salt));
                            transaction.Complete();
                            return RedirectToAction(nameof(ViewOrganisationSettings), "User", new { message = "Ändringarna sparades" });
                        }
                    }
                }
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
