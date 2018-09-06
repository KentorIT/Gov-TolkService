using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequirementType
    {
        [Description("Tolkens kön")]
        Gender = 1,

        [Description("Dialekt")]
        Dialect = 2,

        [Description("Specifik tolk")]
        SpecifiedInterpreter = 3,

        [Description("Ej specifik tolk")]
        DeniedInterpreter = 4,

        [Description("Säkerhetsprövad tolk")]
        HasSecurityClearence = 5,

        [Description("Registerkontrollerad tolk")]
        HasRegisterCheck = 6,

        [Description("Särskilda egenskaper eller kunskaper")]
        SpecialQualities = 7
    }
}

