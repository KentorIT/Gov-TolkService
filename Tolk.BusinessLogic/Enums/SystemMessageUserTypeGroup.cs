using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum SystemMessageUserTypeGroup
    {
        [Description("Samtliga användare")]
        All = 1,

        [Description("Förmedlingsanvändare")]
        BrokerUsers = 2,

        [Description("Myndighetsanvändare")]
        CustomerUsers = 3,

        [Description("Lokala administratörer")]
        SuperUsers = 4,
    }
}
