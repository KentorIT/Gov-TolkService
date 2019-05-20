namespace Tolk.Web.Authorization
{
    public static class Roles
    {
        public const string SystemAdministrator = nameof(SystemAdministrator);
        public const string Impersonator = nameof(Impersonator);
        public const string CentralAdministrator = nameof(CentralAdministrator);
        public const string ApplicationAdministrator = nameof(ApplicationAdministrator);
        public const string CentralOrderHandler = nameof(CentralOrderHandler);
        public const string AdminRoles = nameof(SystemAdministrator) + ", " + nameof(CentralAdministrator);
        public const string AppOrSysAdmin = nameof(SystemAdministrator) + ", " + nameof(ApplicationAdministrator);
    }
}
