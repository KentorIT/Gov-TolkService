using System;
using System.Globalization;

namespace Tolk.BusinessLogic.Utilities
{
    public static class DateExtensions
    {
        public static string ToSwedishString(this DateTime value)
            => value.ToString(CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToSwedishString(this DateTime value, string format)
            => value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToSwedishString(this DateTimeOffset value)
            => value.ToString(CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToSwedishString(this DateTimeOffset value, string format)
            => value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToSwedishString(this TimeSpan value, string format)
            => value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToHoursAndMinutesSwedishString(this TimeSpan ts)
            => $"{ts.ToSwedishString("%h")} tim {((ts.Minutes % 60 == 0) ? string.Empty : (ts.ToSwedishString("%m") + " min"))}";
    }
}
