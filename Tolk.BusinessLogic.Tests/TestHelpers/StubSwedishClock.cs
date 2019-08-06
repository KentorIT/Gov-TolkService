using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            _dateTimeSweden = DateTime.SpecifyKind(DateTime.Parse(dateTimeStringLocal), DateTimeKind.Unspecified).ToDateTimeOffsetSweden();
        }
    }
}
