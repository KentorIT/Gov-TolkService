using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web
{
    public static class Roles
    {
        public const string Admin = nameof(Admin);
        public const string Impersonator = nameof(Impersonator);

        public const string AdminRoleKey = "TolkAdminRole";
        public const string ImpersonatorKey = "TolkImpersonatorRole";
    }
}
