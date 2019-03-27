﻿using System;
using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    [Flags]
    public enum UserType
    {
        [Description("Avropare")]
        OrderCreator = 1,

        [Description("Tolkförmedlare")]
        Broker = 2,

        [Description("Administratör på organisation")]
        OrganisationAdministrator = 4,

        [Description("Tolk")]
        Interpreter = 8,

        [Description("Systemadministratör")]
        SystemAdministrator = 16,
    }
}
