using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{
    public class TolkSecurityStampValidator : SecurityStampValidator<AspNetUser>
    {
        private UserManager<AspNetUser> _userManager;
        private SignInManager<AspNetUser> _signInManager;

        public TolkSecurityStampValidator(
            IOptions<SecurityStampValidatorOptions> options,
            SignInManager<AspNetUser> signInManager,
            UserManager<AspNetUser> userManager,
            ISystemClock clock)
            : base(options, signInManager, clock)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public override async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var oldPrincipal = context.Principal;

            await base.ValidateAsync(context);

            if(context.Principal == null || // Session rejected by base class, everyting is handled.
                ReferenceEquals(oldPrincipal, context.Principal)) // Same principal => not time for refresh yet.
            {
                return;
            }

            var impersonatingUserId = oldPrincipal.FindFirstValue(TolkClaimTypes.ImpersonatingUserId);
            if(impersonatingUserId != null)
            {
                var impersonatingIdentity = new ClaimsIdentity();
                var impersonatingSecurityStamp = oldPrincipal.FindFirstValue(TolkClaimTypes.ImpersonatingUserSecurityStamp);
                impersonatingIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, impersonatingUserId));
                impersonatingIdentity.AddClaim(new Claim(TolkClaimTypes.AspNetSecurityStamp, impersonatingSecurityStamp));
                var impersonatingPrincipal = new ClaimsPrincipal(impersonatingIdentity);

                var impersonatingUser = await _signInManager.ValidateSecurityStampAsync(impersonatingPrincipal);

                if(impersonatingUser != null
                    && await _userManager.IsInRoleAsync(impersonatingUser, Roles.Impersonator))
                {
                    var newIdentity = context.Principal.Identities.Single();
                    ImpersonationHelper.SetupImpersonationClaims(oldPrincipal, newIdentity);
                }
                else
                {
                    context.RejectPrincipal();
                    await _signInManager.SignOutAsync();
                }
            }
        }
    }
}
