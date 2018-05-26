using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    class StubSystemClock : ISystemClock
    {
        private DateTimeOffset _dateTimeUtc;

        public DateTimeOffset UtcNow => _dateTimeUtc;

        public StubSystemClock(string dateTimeStringLocal)
        {
            _dateTimeUtc = DateTime.Parse(dateTimeStringLocal).ToDateTimeOffsetSweden();
        }
    }
}
