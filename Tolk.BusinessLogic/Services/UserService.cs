using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class UserService
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly NotificationService _notificationService;

        public UserService(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            NotificationService notificationService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _options = options.Value;
            _clock = clock;
            _notificationService = notificationService;
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

            plainBody = string.Format(body, link);
            htmlBody = string.Format(HtmlHelper.ToHtmlBreak(body) + NotificationService.NoReplyTextHtml, HtmlHelper.GetButtonDefaultLargeTag(link, "Registrera användarkonto"));

            _notificationService.CreateEmail(user.Email, subject, plainBody, htmlBody);

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

Vid frågor, vänligen kontakta {_options.SupportEmail}";

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

Vid frågor, vänligen kontakta {_options.SupportEmail}";

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

        public async Task LogOnUpdateAsync(int userId, int? updatedByUserId = null)
        {
            AspNetUser currentUserInformation = _dbContext.Users
                            .Include(u => u.NotificationSettings)
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
                RolesHistory = currentUserInformation.Roles.Select(r => new AspNetUserRoleHistoryEntry {
                    RoleId = r.RoleId,
                }).ToList(),
                ClaimsHistory = currentUserInformation.Claims.Select(c => new AspNetUserClaimHistoryEntry {
                    ClaimType = c.ClaimType,
                    ClaimValue = c.ClaimValue,
                }).ToList(),
                NotificationsHistory = currentUserInformation.NotificationSettings.Select(n => new UserNotificationSettingHistoryEntry
                {
                    ConnectionInformation = n.ConnectionInformation,
                    NotificationChannel = n.NotificationChannel,
                    NotificationType = n.NotificationType,
                }).ToList(),
            });
            await _dbContext.SaveChangesAsync();
        }

        public async Task LogUpdatePassword(int userId)
        {
            await _dbContext.AddAsync(new UserAuditLogEntry
            {
                LoggedAt = _clock.SwedenNow,
                UserId = userId,
                UserChangeType = UserChangeType.ChangedPassword
            });
            await _dbContext.SaveChangesAsync();
        }
    }
}
