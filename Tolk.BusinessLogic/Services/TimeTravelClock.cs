using System;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class TimeTravelClock : ISwedishClock
    {
        public long TimeTravelTicks { get; set; }

        public DateTimeOffset SwedenNow =>
            DateTimeOffset.UtcNow.AddTicks(TimeTravelTicks).ToDateTimeOffsetSweden();
    }
}
