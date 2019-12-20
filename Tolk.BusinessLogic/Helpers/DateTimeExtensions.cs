using System;
using System.Linq;

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

        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
