using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public enum PriceRowType
    {
        BasePrice = 1,
        PriceOverMaxTime = 2,
        InconvenientWorkingHours = 3,
        WeekendIWH = 4,
        BigHolidayWeekendIWH = 5,
        LostTime = 6,
        LostTimeIWH = 7
    }
}
