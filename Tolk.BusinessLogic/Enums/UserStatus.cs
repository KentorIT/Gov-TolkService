using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum UserStatus
    {
        [Description("Inaktiva")]
        Inactive = 0,
        [Description("Aktiva")]
        Active = 1,
    }
}
