using System;
using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum DisplayUserRole
    {
        [Description("Förmedlingsanvändare")]
        BrokerUsers = 1,

        [Description("Förmedling central admin")]
        BrokerUserAdministrators = 2,

        [Description("Myndighet beställare")]
        CustomerUsers = 3,

        [Description("Myndighet central admin")]
        CustomerUsersAdministrators = 4
    }
}
