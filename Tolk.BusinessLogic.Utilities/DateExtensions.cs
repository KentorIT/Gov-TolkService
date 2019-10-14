using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public static class DateExtensions
    {
        public static string ToSwedishString(this DateTime value)
        {
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static string ToSwedishString(this DateTime value, string format)
        {
            return value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static string ToSwedishString(this DateTimeOffset value)
        {
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static string ToSwedishString(this DateTimeOffset value, string format)
        {
            return value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static string ToSwedishString(this TimeSpan value, string format)
        {
            return value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));
        }
    }
}
