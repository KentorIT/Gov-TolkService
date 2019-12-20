using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequirementType
    {
        [CustomName("specified_gender")]
        [Description("Tolkens kön")]
        Gender = 1,

        [CustomName("specified_dialect")]
        [Description("Dialekt")]
        Dialect = 2,

        [CustomName("specified_interpreter")]
        [Description("Tillsätt specifik tolk")]
        SpecifiedInterpreter = 3,

        [CustomName("denied_interpreter")]
        [Description("Tillsätt inte specifik tolk")]
        DeniedInterpreter = 4,

        [CustomName("interpreter_has_security_clearence")]
        [Description("Säkerhetsprövad tolk")]
        HasSecurityClearence = 5,

        [CustomName("interpreter_has_register_check")]
        [Description("Registerkontrollerad tolk")]
        HasRegisterCheck = 6,

        [CustomName("specific_qualities")]
        [Description("Särskilda egenskaper eller kunskaper")]
        SpecialQualities = 7
    }
}

