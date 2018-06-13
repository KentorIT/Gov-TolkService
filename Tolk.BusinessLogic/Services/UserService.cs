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

        public async Task SendInvite(AspNetUser user)
        {
            if(user.InterpreterId.HasValue)
            {
                await SendInviteToInterpreter(user);
                return;
            }

            throw new NotImplementedException();
        }
        public async Task SendInviteToInterpreter(AspNetUser user)
        {
            string activationLink = await GenerateActivationLink(user);

            var body =
$@"Hej!

Du har blivit inbjuden till {Constants.SystemName} som tolk av en tolkförmedling.

För att aktivera ditt konto och se uppdrag från förmedlingen, vänligen klicka på
nedanstående länk eller klistra in den i din webbläsare.

{activationLink}

Vid frågor, vänligen kontakta {_options.SupportEmail}";

            _dbContext.Add(new OutboundEmail(
                user.Email,
                $"Du har blivit inbjuden som tolk till {Constants.SystemName}",
                body,
                _clock.SwedenNow));
        }

        private async Task<string> GenerateActivationLink(AspNetUser user)
        {
            // Reset security stamp to kill any existing links.
            await _userManager.UpdateSecurityStampAsync(user);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var activationLink = $"{_options.PublicOrigin}/Account/ConfirmAccount?userId={user.Id}&code={Uri.EscapeDataString(token)}";
            return activationLink;
        }
    }
}
