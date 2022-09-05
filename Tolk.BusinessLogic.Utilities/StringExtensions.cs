using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text;

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

        public static Uri AsUri(this string value) => new Uri(value);

        public static bool ContainsSwedish(this string value, string searchFor) =>
            (value?.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase) ?? -1) >= 0;

        public static bool EqualsSwedish(this string value, string compareWith) => 
            value?.Equals(compareWith, StringComparison.InvariantCultureIgnoreCase) ?? false;

        public static string FormatSwedish(this string format, params object[] args) => 
            string.Format(CultureInfo.GetCultureInfo("sv-SE"), format, args);

        public static string ToSwedishLower(this string value) => 
            value?.ToLower(CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToSwedishUpper(this string value) => 
            value?.ToUpper(CultureInfo.GetCultureInfo("sv-SE"));

        public static bool StartsWithSwedish(this string value, string searhFor) => 
            value?.StartsWith(searhFor, StringComparison.InvariantCultureIgnoreCase) ?? false;

        public static int ToSwedishInt(this string value) => 
            Convert.ToInt32(value, CultureInfo.GetCultureInfo("sv-SE"));

        public static string ToSwedishString(this char value) =>
            value.ToString(CultureInfo.GetCultureInfo("sv-SE"));

        public static TimeSpan ToSwedishTimeSpan(this string value) => TimeSpan.Parse(value, CultureInfo.GetCultureInfo("sv-SE"));

        public static DateTime ToSwedishDateTime(this string value) =>
            Convert.ToDateTime(value, CultureInfo.GetCultureInfo("sv-SE"));

        public static T FromByteArray<T>(this byte[] data) =>
            (data == null || data.Length == 0) ? default : (T)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), typeof(T));

        public static byte[] ToByteArray<T>(this T data) =>
            (data == null) ? null : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

        public static string ToNotHyphenatedFormat(this string value) =>
            value?.Replace("-", string.Empty);
    }
}
