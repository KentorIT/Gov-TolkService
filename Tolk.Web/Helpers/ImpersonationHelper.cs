﻿using System.Security.Claims;
using Tolk.Web.Authorization;

namespace Tolk.Web.Helpers
{
    public static class ImpersonationHelper
    {
        public static void SetupImpersonationClaims(ClaimsPrincipal source, ClaimsIdentity destination)
        {
            if (destination != null)
            {
                destination.AddClaim(new Claim(
                    TolkClaimTypes.ImpersonatingUserId,
                    source.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)
                      ?? source.FindFirstValue(ClaimTypes.NameIdentifier)));

                destination.AddClaim(new Claim(
                    TolkClaimTypes.ImpersonatingUserName,
                    source.FindFirstValue(TolkClaimTypes.ImpersonatingUserName)
                      ?? $"{source.FindFirstValue(TolkClaimTypes.PersonalName)} (inloggad)"));

                destination.AddClaim(new Claim(ClaimTypes.Role, Roles.Impersonator));

                destination.AddClaim(new Claim(
                    TolkClaimTypes.ImpersonatingUserSecurityStamp,
                    source.FindFirstValue(TolkClaimTypes.ImpersonatingUserSecurityStamp)
                      ?? source.FindFirstValue(TolkClaimTypes.AspNetSecurityStamp)));
            }
        }
    }
}
