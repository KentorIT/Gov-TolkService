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
        NA_NoDataLastWeek = 4,

        [Description("Inget data denna period")]
        NA_NoDataThisWeek = 5,
    }
}
