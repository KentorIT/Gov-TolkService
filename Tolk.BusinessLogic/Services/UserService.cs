using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class UserService
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;
        private readonly ILogger _logger;

        public UserService(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            INotificationService notificationService,
            ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _options = options?.Value;
            _clock = clock;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task SendInviteAsync(AspNetUser user)
        {
            string subject = null;
            string body = null;
            string plainBody = null;
            string htmlBody = null;
            NullCheckHelper.ArgumentCheckNull(user, nameof(SendInviteAsync), nameof(UserService));
            if (user.InterpreterId.HasValue)
            {
                (subject, body) = CreateInterpreterInvite();
                if (subject == null || body == null)
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                // Not interpreter => must belong to organization.
                (subject, htmlBody) = CreateOrganizationUserActivation(true);
                (subject, plainBody) = CreateOrganizationUserActivation(false);
                if (subject == null || htmlBody == null || plainBody == null)
                {
                    throw new NotImplementedException();
                }
            }

            var link = await GenerateActivationLinkAsync(user);

            plainBody = plainBody.FormatSwedish(link);
            htmlBody = HtmlHelper.ToHtmlBreak(htmlBody).FormatSwedish(HtmlHelper.GetButtonDefaultLargeTag(link.AsUri(), "Registrera användarkonto"), link);
            _notificationService.CreateEmail(user.Email, subject, plainBody, htmlBody, NotificationType.UserInvitation, addContractInfo: false);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Sent account confirmation link to {userId} ({email})", user.Id, user.Email);
        }

        public async Task SetTemporaryEmail(AspNetUser user, string newEmail, int updatedById, int? impersonatingCreatorId = null)
        {
            NullCheckHelper.ArgumentCheckNull(user, nameof(SetTemporaryEmail), nameof(UserService));
            var emailUser = await _dbContext.Users.GetUserByIdWithTemporaryEmail(user.Id);
            var entry = emailUser.TemporaryChangedEmailEntry ?? new TemporaryChangedEmailEntry();
            entry.EmailAddress = newEmail;
            entry.ExpirationDate = _clock.SwedenNow.AddDays(7);
            entry.UpdatedByUserId = updatedById;
            entry.ImpersonatingUpdatedByUserId = impersonatingCreatorId;
            emailUser.TemporaryChangedEmailEntry = entry;
            await _dbContext.SaveChangesAsync();
        }
        private (string, string) CreateInterpreterInvite()
        {
            var body =
$@"Hej!

Du har blivit inbjuden till {Constants.SystemName} som tolk av en tolkförmedling.

För att se dina tolkuppdrag så måste du registrera ett användarkonto i systemet, vänligen klicka på
nedanstående länk eller klistra in den i din webbläsare.

{{0}}

Vid frågor, vänligen kontakta {_options.Support.FirstLineEmail}";

            var subject = $"Du har blivit inbjuden som tolk till {Constants.SystemName}";

            return (subject, body);
        }

        private (string, string) CreateOrganizationUserActivation(bool isHtml)
        {
            var body =
$@"Hej!

Välkommen till {Constants.SystemName}!

{GetLinkInfo(isHtml)}

Vid frågor, vänligen kontakta {_options.Support.FirstLineEmail}.

Mer information om avropstjänsten hittar du på: {_options.ExternalLinks.CurrentInfo}";

            var subject = $"Aktivering av konto i {Constants.SystemName}";

            return (subject, body);
        }

        private string GetLinkInfo(bool isHtml) => isHtml ?
            $@"För att aktivera ditt konto, vänligen klicka på nedanstående länk: 

{{0}}

Om det inte fungerar att klicka på länken så klistra in länken nedan i en webbläsare:

{{1}}" :
            $@"För att aktivera ditt konto, vänligen klicka på nedanstående länk eller klistra in den i din webbläsare.

{{0}}";

        private async Task<string> GenerateActivationLinkAsync(AspNetUser user)
        {
            // Reset security stamp to kill any existing links.
            await _userManager.UpdateSecurityStampAsync(user);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var activationLink = $"{_options.PublicOrigin}/Account/ConfirmAccount?userId={user.Id}&code={Uri.EscapeDataString(token)}";
            return activationLink;
        }

        public async Task LogCreateAsync(int userId, int? createdById = null, int? impersonatingcreatorId = null)
        {
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UpdatedByUserId = createdById,
                UpdatedByImpersonatorId = impersonatingcreatorId,
                UserChangeType = UserChangeType.Created,
                UserId = userId
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendChangedEmailLink(AspNetUser user, string newEmailAddress, string resetLink, bool changedByAdmin = false)
        {
            NullCheckHelper.ArgumentCheckNull(user, nameof(SendChangedEmailLink), nameof(UserService));
            string message = changedByAdmin ? $"Om du har begärt att få din e-postadress ändrad för '{user.FullName}' så logga in i {Constants.SystemName} med din gamla e-post {user.Email} och klicka eller klistra därefter in länken nedan i webbläsaren för att verifiera ändringen." : $"Om du har bytt e-postadress för '{user.FullName}' så klicka på länken Verifiera e-postadress, om du då får upp en inloggningssida så behöver du logga in med din gamla e-postadress och det vanliga lösenordet, är du redan inloggad i webbläsaren så får du direkt ett meddelande om att e-postadressen är uppdaterad.";
            var bodyPlain =
        $@"Ändring av e-postadress för {Constants.SystemName}

{message}

{resetLink}

Om du inte har bytt/begärt byte av e-postadress kan du radera det här
meddelandet och kontakta
supporten på {_options.Support.FirstLineEmail}.";

            var bodyHtml =
        $@"<h2>Ändring av e-postadress för {Constants.SystemName} </h2>

<div>{message}</div>

<div>{HtmlHelper.GetButtonDefaultLargeTag(resetLink.AsUri(), "Verifiera e-postadress")}</div>

<div>Om du inte har bytt/begärt byte av e-postadress kan du radera det här
meddelandet och kontakta
supporten på {_options.Support.FirstLineEmail}.</div>";

            _notificationService.CreateEmail(
                newEmailAddress,
                $"Ändring av e-postadress för {Constants.SystemName}",
                bodyPlain,
                bodyHtml,
                NotificationType.ChangedEmailVerification,
                isBrokerMail: false,
                addContractInfo: false);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Verification link for changed email sent to {email} for {userId}",
                           newEmailAddress.ToLoggableFormat(), user.Id);
        }

        public async Task LogOnUpdateAsync(int userId, int? updatedByUserId = null, int? impersonatingUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            var roles = _dbContext.UserRoles.GetRolesForUser(userId);
            var claims = await _dbContext.UserClaims.GetClaimsForUser(userId);
            var customerUnits = _dbContext.CustomerUnitUsers.GetCustomerUnitsForUser(userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = impersonatingUpdatedById,
                UserChangeType = UserChangeType.Updated,
                UserHistory = new AspNetUserHistoryEntry(currentUserInformation),
                RolesHistory = await roles.Select(r => new AspNetUserRoleHistoryEntry
                {
                    RoleId = r.RoleId,
                }).ToListAsync(),
                ClaimsHistory = claims.Select(c => new AspNetUserClaimHistoryEntry
                {
                    ClaimType = c.ClaimType,
                    ClaimValue = c.ClaimValue,
                }).ToList(),
                CustomerUnitUsersHistory = await customerUnits.Select(c => new CustomerUnitUserHistoryEntry
                {
                    CustomerUnitId = c.CustomerUnitId,
                    IsLocalAdmin = c.IsLocalAdmin,
                }).ToListAsync(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogOnActivityStateChange(int userId, int? updatedByUserId = null, int? impersonatingUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            var roles = _dbContext.UserRoles.GetRolesForUser(userId);
            var claims = await _dbContext.UserClaims.GetClaimsForUser(userId);
            var customerUnits = _dbContext.CustomerUnitUsers.GetCustomerUnitsForUser(userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = impersonatingUpdatedById,
                UserChangeType = UserChangeType.ChangedActivityState,
                UserHistory = new AspNetUserHistoryEntry(currentUserInformation),             
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogOnMoveAccountAsync(int userId, int? updatedByUserId = null, int? impersonatingUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            var roles = _dbContext.UserRoles.GetRolesForUser(userId);
            var claims = await _dbContext.UserClaims.GetClaimsForUser(userId);
            var customerUnits = _dbContext.CustomerUnitUsers.GetCustomerUnitsForUser(userId);
            var defaultSettings = _dbContext.UserDefaultSettings.GetDefaultSettingsForUser(userId);
            var defaultSettingOrderRequirements = _dbContext.UserDefaultSettingOrderRequirements.GetDefaultSettingOrderRequirementsForUser(userId);

            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = impersonatingUpdatedById,
                UserChangeType = UserChangeType.ChangedOrganisation,
                UserHistory = new AspNetUserHistoryEntry(currentUserInformation),
                RolesHistory = await roles.Select(r => new AspNetUserRoleHistoryEntry
                {
                    RoleId = r.RoleId,
                }).ToListAsync(),
                ClaimsHistory = claims.Select(c => new AspNetUserClaimHistoryEntry
                {
                    ClaimType = c.ClaimType,
                    ClaimValue = c.ClaimValue,
                }).ToList(),
                CustomerUnitUsersHistory = await customerUnits.Select(c => new CustomerUnitUserHistoryEntry
                {
                    CustomerUnitId = c.CustomerUnitId,
                    IsLocalAdmin = c.IsLocalAdmin,
                }).ToListAsync(),
                DefaultsHistory = await defaultSettings.Select(n => new UserDefaultSettingHistoryEntry
                {
                    DefaultSettingType = n.DefaultSettingType,
                    Value = n.Value
                }).ToListAsync(),
                DefaultOrderRequirementsHistory = await defaultSettingOrderRequirements.Select(n => new UserDefaultSettingsOrderRequirementHistoryEntry
                {
                    RequirementType = n.RequirementType,
                    Description = n.Description,
                    IsRequired = n.IsRequired
                }).ToListAsync(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public void RemoveAllDefaultSettings(int userId, int? updatedByUserId = null, int? impersonatingUpdatedById = null)
        {
            _dbContext.UserDefaultSettings.RemoveRange(_dbContext.UserDefaultSettings.Where(uds => uds.UserId == userId));
            _dbContext.UserDefaultSettingOrderRequirements.RemoveRange(_dbContext.UserDefaultSettingOrderRequirements.Where(uds => uds.UserId == userId));
        }

        public async Task LogNotificationSettingsUpdateAsync(int userId, int? updatedByUserId = null, int? impersonatorUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users.GetUserById(userId);
            currentUserInformation.NotificationSettings = await _dbContext.UserNotificationSettings.GetNotificationSettingsForUser(userId).ToListAsync();
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = impersonatorUpdatedById,
                UserChangeType = UserChangeType.UpdatedNotificationSettings,
                NotificationsHistory = currentUserInformation.NotificationSettings.Select(n => new UserNotificationSettingHistoryEntry
                {
                    ConnectionInformation = n.ConnectionInformation,
                    NotificationChannel = n.NotificationChannel,
                    NotificationType = n.NotificationType,
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }
        public async Task LogDefaultSettingsUpdateAsync(int userId, int? updatedByUserId = null, int? impersonatorUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users.GetUserById(userId);
            currentUserInformation.DefaultSettings = await _dbContext.UserDefaultSettings.GetDefaultSettingsForUser(userId).ToListAsync();
            currentUserInformation.DefaultSettingOrderRequirements = await _dbContext.UserDefaultSettingOrderRequirements.GetDefaultSettingOrderRequirementsForUser(userId).ToListAsync();
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = impersonatorUpdatedById,
                UserChangeType = UserChangeType.UpdatedDefaultSettings,
                DefaultsHistory = currentUserInformation.DefaultSettings.Select(n => new UserDefaultSettingHistoryEntry
                {
                    DefaultSettingType = n.DefaultSettingType,
                    Value = n.Value
                }).ToList(),
                DefaultOrderRequirementsHistory = currentUserInformation.DefaultSettingOrderRequirements.Select(n => new UserDefaultSettingsOrderRequirementHistoryEntry
                {
                    RequirementType = n.RequirementType,
                    Description = n.Description,
                    IsRequired = n.IsRequired
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogCustomerUnitUserUpdateAsync(int userId, int? updatedByUserId = null, int? impersonatorUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users.GetUserById(userId);
            currentUserInformation.CustomerUnits = await _dbContext.CustomerUnitUsers.GetCustomerUnitsForUser(userId).ToListAsync();
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = impersonatorUpdatedById,
                UserChangeType = UserChangeType.UpdatedCustomerUnitUserOnly,
                CustomerUnitUsersHistory = currentUserInformation.CustomerUnits?.Select(c => new CustomerUnitUserHistoryEntry
                {
                    CustomerUnitId = c.CustomerUnitId,
                    IsLocalAdmin = c.IsLocalAdmin,
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogUpdateEmailAsync(int userId, int? updatedByUserId = null, int? imppersonatorUpdatedById = null)
        {
            AspNetUser currentUserInformation = await _dbContext.Users
                .SingleOrDefaultAsync(u => u.Id == userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UpdatedByImpersonatorId = imppersonatorUpdatedById,
                UserChangeType = UserChangeType.ChangedEmail,
                UserHistory = new AspNetUserHistoryEntry(currentUserInformation),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogUpdatePasswordAsync(int userId, int? impersonatingUpdatedId = null)
        {
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByImpersonatorId = impersonatingUpdatedId,
                UserChangeType = UserChangeType.ChangedPassword
            });
            await _dbContext.SaveChangesAsync();
        }                
     
        public async Task LogLoginAsync(int userId)
        {
            await _dbContext.AddAsync(new UserLoginLogEntry { LoggedInAt = _clock.SwedenNow, UserId = userId });
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> TryActivateUser(AspNetUser user, bool initiatedByAdmin = false)
        {
            if (user.IsActive) 
            { 
                return true;
            }
            if (!user.IsActive && (user.ActivityStateChangedByAdmin ?? true) && !initiatedByAdmin)
            {
                return false;
            }
            await LogOnActivityStateChange(user.Id);
            user.IsActive = true;
            SetActivityStateChange(user, initiatedByAdmin, newActivityState: true);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public void SetActivityStateChange(AspNetUser user, bool activityStateChangedByAdmin, bool newActivityState)
        {
            user.ActivityStateChangedByAdmin = activityStateChangedByAdmin;            
            var deactivationInitiatedBy = activityStateChangedByAdmin ? "by an admin" : "by the system";
            _logger.LogInformation("User with id: {userId}, activity state changed to: {ActivityState} {initiatedBy}", user.Id, newActivityState, deactivationInitiatedBy);
        }

        public async Task<AspNetUser> GetUserWithDefaultSettings(int userId)
        {
            var user = await _userManager.Users.GetUserById(userId);
            user.DefaultSettings = await _dbContext.UserDefaultSettings.GetDefaultSettingsForUser(user.Id).ToListAsync();
            user.DefaultSettingOrderRequirements = await _dbContext.UserDefaultSettingOrderRequirements.GetDefaultSettingOrderRequirementsForUser(user.Id).ToListAsync();
            user.CustomerUnits = await _dbContext.CustomerUnitUsers.GetCustomerUnitsWithCustomerUnitForUser(user.Id).ToListAsync();
            return user;
        }

        public string GenerateUserName(string firstName, string lastName, string prefix)
        {
            NullCheckHelper.ArgumentCheckNull(firstName, nameof(GenerateUserName), nameof(UserService));
            NullCheckHelper.ArgumentCheckNull(lastName, nameof(GenerateUserName), nameof(UserService));
            NullCheckHelper.ArgumentCheckNull(prefix, nameof(GenerateUserName), nameof(UserService));
            string userNameStart = $"{prefix.GetPrefix(prefix.Length)}{firstName.GetPrefix()}{lastName.GetPrefix()}";
            var users = _dbContext.Users.Where(u => EF.Functions.Like(u.NormalizedUserName, $"{userNameStart}%")).Select(u => u.NormalizedUserName).ToList();
            for (int i = 1; i < 100; ++i)
            {
                var userName = $"{userNameStart}{i.ToSwedishString("D2")}";
                if (!users.Contains(userName.ToSwedishUpper()))
                {
                    return userName;
                }
            }
            _logger.LogWarning("There are at least 100 users starting with the string {userName}.", userNameStart);
            _notificationService.CreateEmail(
                _options.Support.SecondLineEmail,
                $"Det har skapats mer än hundra användare med prefix {userNameStart}", "Detta kan vara ett tecken på att systemet är under attack...",
                null,
                NotificationType.GeneraratedUserPrefixLimitWarning,
                addContractInfo: false
            );
            for (int i = 1; i < 1000; ++i)
            {
                var userName = $"{userNameStart}{i.ToSwedishString("D3")}";
                if (!users.Contains(userName.ToSwedishUpper()))
                {
                    return userName;
                }
            }
            _logger.LogWarning("There are at least 1000 users starting with the string {userName}.", userNameStart);

            throw new NotSupportedException("Too many users starting with the string {userNameStart}.");
        }

        public bool IsUniqueEmail(string email, int? userId = null, int? customerUnitId = null)
        {
            var emailIsUniqueForUsers = !_dbContext.Users.Any(u => !u.IsApiUser &&
                     (u.NormalizedEmail == email.ToUpper() ||
                     (u.TemporaryChangedEmailEntry.EmailAddress.ToUpper() == email.ToUpper() && u.TemporaryChangedEmailEntry.UserId != userId)));
            var emailIsUniqueForUnits = !_dbContext.CustomerUnits.Any(cu => cu.Email.ToUpper() == email.ToUpper() && cu.CustomerUnitId != customerUnitId);
            return emailIsUniqueForUsers && emailIsUniqueForUnits;
        }

        public async Task HandleInactiveUsers()
        {
            if(!_options.UserInactivity.EnableAutomaticDeactivation)
            {
                return;
            }

            var now = _clock.SwedenNow.Date;
            var cutoff = now.AddMonths(-_options.UserInactivity.InactivationThresholdMonths).AddDays(_options.UserInactivity.NotifyDaysBeforeDeactivation[0]).Date.ToDateTimeOffsetSweden();

            var inactiveUsers = await _dbContext.Users
                .WhereNotLoggedInSince(cutoff)
                .Select(u => new
                {
                    User = u,
                    // Handle if deactivation wasn't done on the day so it wasn't deactivated and should be deactivated (key < 0 here)  
                    DeactivationIn = Math.Max(0, (u.LastLoginAt.Value.AddMonths(_options.UserInactivity.InactivationThresholdMonths) - now).Days)
                }).ToListAsync();

            var inactiveUsersDictionary = inactiveUsers.GroupBy(u => u.DeactivationIn).ToDictionary(g => g.Key, g => g.Select(iu => iu.User).ToList());

            await HandleDeactivationReminders(inactiveUsersDictionary);

            await DeactivateUsers(now, inactiveUsersDictionary.TryGetValue(0, out var usersFromDictionary) ? usersFromDictionary : new List<AspNetUser>());

            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleDeactivationReminders(Dictionary<int,List<AspNetUser>> inactiveUsersDictionary)
        {
            foreach (var threshHoldDays in _options.UserInactivity.NotifyDaysBeforeDeactivation)
            {
                if (inactiveUsersDictionary.TryGetValue(threshHoldDays, out var usersToNotify))
                {
                    await SendInactivityMail(usersToNotify, threshHoldDays);
                }
            }
        }

        private async Task DeactivateUsers(DateTimeOffset now, List<AspNetUser> usersToDeactivate)
        {                
            // If user hasn't logged in in >=6 months, it should be deactivated
            var cutoff = now.AddMonths(-6).Date.ToDateTimeOffsetSweden();

            var neverLoggedInusers = await _dbContext.Users
              .WhereNeverLoggedInAndCreatedPriorToDate(cutoff)
              .ToListAsync();

            usersToDeactivate.AddRange(neverLoggedInusers);

            foreach (var user in usersToDeactivate)
            {
                await LogOnActivityStateChange(user.Id);
                user.IsActive = false;
                SetActivityStateChange(user, activityStateChangedByAdmin: false, newActivityState: false);
            }
            
        }

        private async Task SendInactivityMail(List<AspNetUser> users, int daysUntilDeactivation)
        {
            foreach (var user in users)
            {
                string subject = null;
                string plainBody = null;
                string htmlBody = null;
                var userEmail = user.Email;
                (subject, plainBody,htmlBody) = CreateInactivityEmail(daysUntilDeactivation);

                _notificationService.CreateEmail(
                  userEmail,
                  subject,
                  plainBody,
                  htmlBody,
                  NotificationType.DeactivationReminder,
                  isBrokerMail: false,
                  addContractInfo: false);
                _logger.LogInformation("User inactivity reminder sent to {email} for user with id: {userId}", userEmail.ToLoggableFormat(), user.Id);
            }

            await _dbContext.SaveChangesAsync();
        }

        private (string, string, string) CreateInactivityEmail(int daysUntilDeactivation)
        {                                    
            var subject = $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras";
            var plainBody =
$@"Hej!

Du har snart inte loggat in på ditt konto för: {Constants.SystemName} på {_options.UserInactivity.InactivationThresholdMonths} månader, ditt konto
kommer därför att inaktiveras om {daysUntilDeactivation} dagar.

För att undvika att kontot inaktiveras behöver du logga in i tjänsten på länken här:

{_options.PublicOrigin}/

Vid frågor, vänligen kontakta {_options.Support.FirstLineEmail}";

            var htmlBody = $@"
<h1>Hej!</h1>

Du har snart inte loggat in på ditt konto för: {Constants.SystemName} på <b>{_options.UserInactivity.InactivationThresholdMonths}</b> månader, ditt konto
kommer därför att inaktiveras om <b>{daysUntilDeactivation}</b> dagar.

För att undvika att kontot inaktiveras behöver du logga in i tjänsten på länken här:

<div>{HtmlHelper.GetButtonDefaultLargeTag(_options.PublicOrigin.AsUri(), "Tolkavropstjänsten")}</div>

Om det inte fungerar att klicka på länken så klistra in länken nedan i en webbläsare:

{_options.PublicOrigin.AsUri()}

Vid frågor, vänligen kontakta <b>{_options.Support.FirstLineEmail}</b>
";                        
            return (subject, plainBody, HtmlHelper.ToHtmlBreak(htmlBody));
        }
    }
}