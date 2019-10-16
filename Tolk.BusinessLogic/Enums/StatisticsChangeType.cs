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

        [Description("Inget data förra perioden")]
        NANoDataLastWeek = 4,

        [Description("Inget data denna period")]
        NANoDataThisWeek = 5,
    }
}
