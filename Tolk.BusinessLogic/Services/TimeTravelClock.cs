using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Services
{
    public class TimeTravelClock : ISystemClock
    {
        public long TimeTravelTicks { get; set; }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow.AddTicks(TimeTravelTicks);
    }
}
