using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.Web.Authorization;

namespace Tolk.Web.Helpers
{
    public static class UserExtensions
    {
        public static int GetCustomerOrganisationId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirstValue(TolkClaimTypes.CustomerOrganisationId));
        }
        public static int GetBrokerId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirstValue(TolkClaimTypes.BrokerId));
        }

        public static int GetUserId(this ClaimsPrincipal user)
        {
            // This logic is present int he UserManager, but I'm and repeating it here
            // to avoid having to get hold of an instance through DI.

            return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        public static int? GetImpersonatorId(this ClaimsPrincipal user)
        {
            if(int.TryParse(user.FindFirstValue(TolkClaimTypes.ImpersonatingUserId), out int result))
            {
                return result;
            }
            return null;
        }

        public static int GetInterpreterId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirstValue(TolkClaimTypes.InterpreterId));
        }
    }
}
