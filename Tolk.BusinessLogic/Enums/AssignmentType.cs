using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum AssignmentType
    {
        [CustomName("interpretation")]
        [Description("Tolkning")]
        Interpretation = 1,
        [CustomName("avista")]
        [Description("Tolkning inkl. avista")]
        Avista = 2
    }
}
