using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Authorization
{
    public static class TolkClaimTypes
    {
        public const string ImpersonatingUserId = nameof(ImpersonatingUserId);

        public const string ImpersonatingUserName = nameof(ImpersonatingUserName);

        public const string ImpersonatingUserSecurityStamp = nameof(ImpersonatingUserSecurityStamp);

        public const string AspNetSecurityStamp = "AspNet.Identity.SecurityStamp";

        public const string CustomerOrganisationId = nameof(CustomerOrganisationId);

        public const string BrokerId = nameof(BrokerId);

        public const string InterpreterId = nameof(InterpreterId);

        public const string PersonalName = nameof(PersonalName);

        public const string IsPasswordSet = nameof(IsPasswordSet);
    }
}
