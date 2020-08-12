using System.Security.Claims;
using Tolk.Web.Api.Services;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Helpers
{
    public static class UserExtensions
    {
        public static int? TryGetBrokerId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(TolkClaimTypes.BrokerId), out int id))
            {
                return id;
            }
            return null;
        }
        public static int? TryGetCustomerId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(TolkClaimTypes.CustomerOrganisationId), out int id))
            {
                return id;
            }
            return null;
        }
        public static int UserId(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier).ToSwedishInt();
        }
    }
}

