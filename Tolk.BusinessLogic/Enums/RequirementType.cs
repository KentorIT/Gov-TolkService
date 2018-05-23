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

        [Description("Önskad tolk")]
        SpecifiedInterpreter = 2,

        [Description("Önskar ej tolk")]
        DeniedInterpreter = 3,

        [Description("Tolken har genomgått säkerhetsprövning")]
        HasSecurityClearence = 4,

        [Description("Övrigt")]
        Other = 5,
    }
}
