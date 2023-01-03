using System;
using System.Globalization;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;

namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    class StubSwedishClock : ISwedishClock
    {
        private DateTimeOffset _dateTimeSweden;

        public DateTimeOffset SwedenNow => _dateTimeSweden;

        public StubSwedishClock(string dateTimeStringLocal)
        {
            DateTimeFormatInfo dtfi = new CultureInfo("sv-SE").DateTimeFormat;
            _dateTimeSweden = DateTimeOffset.Parse(dateTimeStringLocal, dtfi).ToDateTimeOffsetSweden();
        }
    }
}
