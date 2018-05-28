using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo timeZoneInfo =
            TimeZoneInfo.GetSystemTimeZones().Single(tzi => tzi.Id == "W. Europe Standard Time");

        /// <summary>
        /// Convert a raw user-inserted date time into a DateTimeOffset, observering
        /// Swedish rules for daylight savings time.
        /// </summary>
        /// <param name="rawDateTime">Time and date as entered from user</param>
        /// <returns>DateTimeOffset</returns>
        public static DateTimeOffset ToDateTimeOffsetSweden(this DateTime rawDateTime)
        {
            var timeZoneOffset = timeZoneInfo.GetUtcOffset(rawDateTime);

            return new DateTimeOffset(rawDateTime, timeZoneOffset);
        }

        public static DateTimeOffset ToDateTimeOffsetSweden(this DateTimeOffset dateTimeOffset)
        {
            var timezoneOffset = timeZoneInfo.GetUtcOffset(dateTimeOffset);

            return dateTimeOffset.ToOffset(timezoneOffset);
        }
    }
}
