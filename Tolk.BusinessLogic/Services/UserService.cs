using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
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
            _options = options.Value;
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

            if (user.InterpreterId.HasValue)
            {
                (subject, body) = CreateInterpreterInvite();
            }
            else
            {
                // Not interpreter => must belong to organization.
                (subject, body) = CreateOrganizationUserActivation();
            }

            if (subject == null || body == null)
            {
                throw new NotImplementedException();
            }

            var link = await GenerateActivationLinkAsync(user);

            plainBody = body.FormatSwedish(link);
            htmlBody = HtmlHelper.ToHtmlBreak(body).FormatSwedish(HtmlHelper.GetButtonDefaultLargeTag(link, "Registrera användarkonto"));

            _notificationService.CreateEmail(user.Email, subject, plainBody, htmlBody);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Sent account confirmation link to {userId} ({email})", user.Id, user.Email);
        }

        public async Task SetTemporaryEmail(AspNetUser user, string newEmail)
        {
            var emailUser = await _dbContext.Users.Include(u => u.TemporaryChangedEmailEntry).SingleAsync(u => u.Id == user.Id);

            if (emailUser.TemporaryChangedEmailEntry != null)
            {
                emailUser.TemporaryChangedEmailEntry.EmailAddress = newEmail;
                emailUser.TemporaryChangedEmailEntry.ExpirationDate = _clock.SwedenNow.AddDays(7);
            }
            else
            {
                user.TemporaryChangedEmailEntry = new TemporaryChangedEmailEntry { EmailAddress = newEmail, User = user, ExpirationDate = _clock.SwedenNow.AddDays(7) };
            }
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

        private (string, string) CreateOrganizationUserActivation()
        {
            var body =
$@"Hej!

Välkommen till {Constants.SystemName}!

För att aktivera ditt konto, vänligen klicka på nedanstående länk eller klistra
in den i din webbläsare.

{{0}}

Vid frågor, vänligen kontakta {_options.Support.FirstLineEmail}";

            var subject = $"Aktivering av konto i {Constants.SystemName}";

            return (subject, body);
        }

        private async Task<string> GenerateActivationLinkAsync(AspNetUser user)
        {
            // Reset security stamp to kill any existing links.
            await _userManager.UpdateSecurityStampAsync(user);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var activationLink = $"{_options.PublicOrigin}/Account/ConfirmAccount?userId={user.Id}&code={Uri.EscapeDataString(token)}";
            return activationLink;
        }

        public async Task LogCreateAsync(int userId, int? createdById = null)
        {
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UpdatedByUserId = createdById,
                UserChangeType = UserChangeType.Created,
                UserId = userId
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendChangedEmailLink(AspNetUser user, string newEmailAddress, string resetLink, bool changedByAdmin = false)
        {
            string message = changedByAdmin ? $"Om du har begärt byte av e-postadress för '{user.FullName}' så logga in i {Constants.SystemName} med din gamla e-post {user.Email} och klistra därefter in länken nedan i webbläsaren för att verifiera ändringen." : $"Om du har bytt e-postadress för '{user.FullName}' klicka eller klistra in länken nedan i webbläsaren för att verifiera ändringen.";
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

<div>{HtmlHelper.GetButtonDefaultLargeTag(resetLink, "Verifiera e-postadress")}</div>

<div>Om du inte har bytt/begärt byte av e-postadress kan du radera det här
meddelandet och kontakta
supporten på {_options.Support.FirstLineEmail}.</div>";

            _notificationService.CreateEmail(
                newEmailAddress,
                $"Ändring av e-postadress för {Constants.SystemName}",
                bodyPlain,
                bodyHtml,
                false,
                false);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Verification link for changed email sent to {email} for {userId}",
                           newEmailAddress, user.Id);
        }

        public async Task LogOnUpdateAsync(int userId, int? updatedByUserId = null)
        {
            AspNetUser currentUserInformation = _dbContext.Users
                            .Include(u => u.Claims)
                            .Include(u => u.Roles)
                            .SingleOrDefault(u => u.Id == userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UserChangeType = UserChangeType.Updated,
                UserHistory = new AspNetUserHistoryEntry(currentUserInformation),
                RolesHistory = currentUserInformation.Roles.Select(r => new AspNetUserRoleHistoryEntry
                {
                    RoleId = r.RoleId,
                }).ToList(),
                ClaimsHistory = currentUserInformation.Claims.Select(c => new AspNetUserClaimHistoryEntry
                {
                    ClaimType = c.ClaimType,
                    ClaimValue = c.ClaimValue,
                }).ToList(),
                CustomerUnitUsersHistory = currentUserInformation.CustomerUnits?.Select(c => new CustomerUnitUserHistoryEntry
                {
                    CustomerUnitId = c.CustomerUnitId,
                    IsLocalAdmin = c.IsLocalAdmin,
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogNotificationSettingsUpdateAsync(int userId, int? updatedByUserId = null)
        {
            AspNetUser currentUserInformation = _dbContext.Users
                            .Include(u => u.NotificationSettings)
                            .SingleOrDefault(u => u.Id == userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
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
        public async Task LogDefaultSettingsUpdateAsync(int userId, int? updatedByUserId = null)
        {
            AspNetUser currentUserInformation = _dbContext.Users
                            .Include(u => u.DefaultSettings)
                            .SingleOrDefault(u => u.Id == userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UserChangeType = UserChangeType.UpdatedDefaultSettings,
                DefaultsHistory = currentUserInformation.DefaultSettings.Select(n => new UserDefaultSettingHistoryEntry
                {
                    DefaultSettingType = n.DefaultSettingType,
                    Value = n.Value
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogCustomerUnitUserUpdateAsync(int userId, int? updatedByUserId = null)
        {
            AspNetUser currentUserInformation = _dbContext.Users
                .Include(u => u.CustomerUnits)
                .SingleOrDefault(u => u.Id == userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UserChangeType = UserChangeType.UpdatedCustomerUnitUserOnly,
                CustomerUnitUsersHistory = currentUserInformation.CustomerUnits?.Select(c => new CustomerUnitUserHistoryEntry
                {
                    CustomerUnitId = c.CustomerUnitId,
                    IsLocalAdmin = c.IsLocalAdmin,
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogUpdateEmailAsync(int userId, int? updatedByUserId = null)
        {
            AspNetUser currentUserInformation = _dbContext.Users
                .SingleOrDefault(u => u.Id == userId);
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UpdatedByUserId = updatedByUserId,
                UserChangeType = UserChangeType.ChangedEmail,
                UserHistory = new AspNetUserHistoryEntry(currentUserInformation),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogUpdatePasswordAsync(int userId)
        {
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UserChangeType = UserChangeType.ChangedPassword
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogLoginAsync(int userId)
        {
            await _dbContext.AddAsync(new UserLoginLogEntry { LoggedInAt = _clock.SwedenNow, UserId = userId });
            await _dbContext.SaveChangesAsync();
        }

        public string GenerateUserName(string firstName, string lastName, string prefix)
        {
            string userNameStart = $"{prefix.GetPrefix(prefix.Length)}{firstName.GetPrefix()}{lastName.GetPrefix()}";
            var users = _dbContext.Users.Where(u => u.NormalizedUserName.StartsWithSwedish(userNameStart)).Select(u => u.NormalizedUserName).ToList();
            for (int i = 1; i < 100; ++i)
            {
                var userName = $"{userNameStart}{i.ToSwedishString("D2")}";
                if (!users.Contains(userName.ToSwedishUpper()))
                {
                    return userName;
                }
            }
            _logger.LogWarning("There are at least 100 users starting with the string {userName}.", userNameStart);
            _notificationService.CreateEmail(_options.Support.SecondLineEmail, $"Det har skapats mer än hundra användare med prefix {userNameStart}", "Detta kan vara ett tecken på att systemet är under attack...", addContractInfo: false);
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

        public bool IsUniqueEmail(string email)
        {
#pragma warning disable CA1304 // Ef gets better at filtering at server...
            return !_dbContext.Users.Any(u => !u.IsApiUser &&
                     (u.NormalizedEmail == email.ToUpper() ||
                     u.TemporaryChangedEmailEntry.EmailAddress.ToUpper() == email.ToUpper()));
#pragma warning restore CA1304 // 
        }
    }
}
