using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Authorization
{
    public static class Roles
    {
        public const string SystemAdministrator = nameof(SystemAdministrator);
        public const string Impersonator = nameof(Impersonator);
        public const string CentralAdministrator = nameof(CentralAdministrator);
        public const string AdminRoles = nameof(SystemAdministrator) + ", " + nameof(CentralAdministrator);
    }
}
