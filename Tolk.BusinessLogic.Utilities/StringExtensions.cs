using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Runtime;

namespace Tolk.BusinessLogic.Utilities
{
    public static class StringExtensions
    {
        public static string ToLowerFirstChar(this string input)
        {
            string newString = input;
            if (!string.IsNullOrEmpty(newString) && char.IsUpper(newString[0]))
                newString = char.ToLower(newString[0], CultureInfo.GetCultureInfo("sv-SE")) + newString.Substring(1);
            return newString;
        }

        public static Uri AsUri(this string value)
        {
            return new Uri(value);
        }

        public static bool ContainsSwedish(this string value, string searchFor)
        {
            return (value?.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) ?? -1) >= 0;
        }

        public static bool EqualsSwedish(this string value, string compareWith)
        {
            return value?.Equals(compareWith, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        public static string FormatSwedish(this string format, params object[] args)
        {

            return string.Format(CultureInfo.GetCultureInfo("sv-SE"), format, args);
        }

        public static string ToSwedishLower(this string value)
        {
            return value?.ToLower(CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static string ToSwedishUpper(this string value)
        {
            return value?.ToUpper(CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static bool StartsWithSwedish(this string value, string searhFor)
        {
            return value?.StartsWith(searhFor, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        public static int ToSwedishInt(this string value)
        {
            return Convert.ToInt32(value, CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static string ToSwedishString(this char value)
        {
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static TimeSpan ToSwedishTimeSpan(this string value)
        {
            return TimeSpan.Parse(value, CultureInfo.GetCultureInfo("sv-SE"));
        }

        public static DateTime ToSwedishDateTime(this string value)
        {
            return Convert.ToDateTime(value, CultureInfo.GetCultureInfo("sv-SE"));
        }
    }
}
