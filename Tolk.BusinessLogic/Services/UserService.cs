using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class UserService
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;

        public UserService(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _options = options.Value;
            _clock = clock;
        }

        public async Task SendInviteAsync(AspNetUser user)
        {
            string subject = null;
            string body = null;

            if(user.InterpreterId.HasValue)
            {
                (subject, body) = CreateInterpreterInvite();
            }
            else
            {
                // Not interpreter => must belong to organization.
                (subject, body) = CreateOrganizationUserActivation();
            }

            if(subject == null || body == null)
            {
                throw new NotImplementedException();
            }

            body = string.Format(body, await GenerateActivationLinkAsync(user));

            _dbContext.Add(new OutboundEmail(
                user.Email,
                subject,
                body,
                _clock.SwedenNow));

            await _dbContext.SaveChangesAsync();
        }

        private (string, string) CreateInterpreterInvite()
        {
            var body =
$@"Hej!

Du har blivit inbjuden till {Constants.SystemName} som tolk av en tolkförmedling.

För att aktivera ditt konto och se uppdrag från förmedlingen, vänligen klicka på
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
    }
}
