using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
