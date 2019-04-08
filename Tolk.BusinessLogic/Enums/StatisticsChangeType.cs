using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum StatisticsChangeType
    {
        [Description("+")]
        Increasing = 1,

        [Description("-")]
        Decreasing = 2,

        [Description("+/-")]
        Unchanged = 3,

        [Description("N/A")]
        NotApplicable = 4,
    }
}
