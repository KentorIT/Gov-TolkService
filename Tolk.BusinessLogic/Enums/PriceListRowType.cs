using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum PriceListRowType
    {
        [CustomName("base_price")]
        [Description("Arvode exkl. moms och sociala avgifter")]
        BasePrice = 1,

        [CustomName("price_over_max_time")]
        [Description("Arvode per påbörjad halvtimme för uppdrag som överstiger 5,5 tim")]
        PriceOverMaxTime = 2,

        [CustomName("inconvenient_working_hours")]
        [Description("Tillägg per påbörjad halvtimme obekväm arbetstid må-fre")]
        InconvenientWorkingHours = 3,

        [CustomName("weekend_inconvenient_working_hours")]
        [Description("Tillägg per påbörjad halvtimme obekväm arbetstid helg")]
        WeekendIWH = 4,

        [CustomName("big_holiday_weekend_inconvenient_working_hours")]
        [Description("Tillägg per påbörjad halvtimme för obekväm arbetstid storhelg")]
        BigHolidayWeekendIWH = 5,

        [CustomName("lost_time")]
        [Description("Ersättning för tidsspillan per påbörjad timme under ordinarie arbetstid")]
        LostTime = 6,

        [CustomName("lost_time_inconvenient_working_hours")]
        [Description("Ersättning för tidsspillan per påbörjad halvtimme under obekväm arbetstid")]
        LostTimeIWH = 7,
    }
}
