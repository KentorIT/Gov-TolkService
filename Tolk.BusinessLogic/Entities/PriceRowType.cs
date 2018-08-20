using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;


namespace Tolk.BusinessLogic.Entities
{
    public enum PriceRowType
    {

        [Description("Arvode exkl. moms och sociala avgifter")]
        BasePrice = 1,

        [Description("Arvode per påbörjad halvtimme för uppdrag som överstiger 5,5 tim")]
        PriceOverMaxTime = 2,

        [Description("Tillägg per påbörjad halvtimme obekväm arbetstid må-fre")]
        InconvenientWorkingHours = 3,

        [Description("Tillägg per påbörjad halvtimme obekväm arbetstid helg")]
        WeekendIWH = 4,

        [Description("Tillägg per påbörjad halvtimme för obekväm arbetstid storhelg")]
        BigHolidayWeekendIWH = 5,

        [Description("Ersättning för tidsspillan per påbörjad timme under ordinarie arbetstid")]
        LostTime = 6,

        [Description("Ersättning för tidsspillan per påbörjad halvtimme under obekväm arbetstid")]
        LostTimeIWH = 7

    }
}
