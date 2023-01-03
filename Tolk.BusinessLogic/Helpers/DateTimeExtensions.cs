using System;
using System.Linq;

namespace Tolk.BusinessLogic.Helpers
{
    public static class DateTimeExtensions
    {
        //Try getting timezone by windows name standard first, and if that fails, try the linux way.
        // used if running tests on non-windows machines
        private static readonly TimeZoneInfo timeZoneInfo =
            TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time") ??
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

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

        public static DateTimeOffset AddDate(this DateTimeOffset baseDate, string value)
        {
            var date = DateTime.Parse(value);
            return baseDate.AddYears(date.Year - 1)
                .AddMonths(date.Month - 1)
                .AddDays(date.Day - 1);
        }

        public static DateTimeOffset AddTime(this DateTimeOffset baseTime, string value)
        {
            var time = DateTime.Parse(value);
            return baseTime.AddHours(time.Hour)
                .AddMinutes(time.Minute)
                .AddSeconds(time.Second);
        }

        public static DateTimeOffset ClearSeconds(this DateTimeOffset baseTime)
            => baseTime.AddSeconds(-baseTime.Second);
    }
}
