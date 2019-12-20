using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;

namespace Tolk.Web.Helpers
{
    public static class UserExtensions
    {
        public static int GetCustomerOrganisationId(this ClaimsPrincipal user)
        {
            return user.TryGetCustomerOrganisationId().Value;
        }

        public static int? TryGetCustomerOrganisationId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(TolkClaimTypes.CustomerOrganisationId), out int id))
            {
                return id;
            }
            return null;
        }

        public static int GetBrokerId(this ClaimsPrincipal user)
        {
            return user.TryGetBrokerId().Value;
        }

        public static int? TryGetBrokerId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(TolkClaimTypes.BrokerId), out int id))
            {
                return id;
            }
            return null;
        }

        public static bool IsImpersonated(this ClaimsPrincipal user)
        {
            return (user.TryGetImpersonatorId() > 0 ? user.TryGetImpersonatorId() != user.GetUserId() : false);
        }

        public static int GetUserId(this ClaimsPrincipal user)
        {
            // This logic is present int he UserManager, but I'm and repeating it here
            // to avoid having to get hold of an instance through DI.

            return user.FindFirstValue(ClaimTypes.NameIdentifier).ToSwedishInt();
        }

        public static int? TryGetImpersonatorId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(TolkClaimTypes.ImpersonatingUserId), out int result))
            {
                return result;
            }
            return null;
        }

        public static int GetInterpreterId(this ClaimsPrincipal user)
        {
            return user.TryGetInterpreterId().Value;
        }

        public static int? TryGetInterpreterId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(TolkClaimTypes.InterpreterId), out int id))
            {
                return id;
            }
            return null;
        }

        public static IEnumerable<int> TryGetAllCustomerUnits(this ClaimsPrincipal user)
        {
            if (TryGetCustomerOrganisationId(user) != null)
            {
                return user?.FindAll(TolkClaimTypes.AllCustomerUnits).Select(c => c.Value.ToSwedishInt());
            }
            return null;
        }

        public static IEnumerable<int> TryGetLocalAdminCustomerUnits(this ClaimsPrincipal user)
        {
            if (TryGetCustomerOrganisationId(user) != null)
            {
                return user?.FindAll(TolkClaimTypes.LocalAdminCustomerUnits).Select(c => c.Value.ToSwedishInt());
            }
            return null;
        }
    }
}
