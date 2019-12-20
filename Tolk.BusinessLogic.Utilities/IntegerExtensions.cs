using System.Globalization;

namespace Tolk.BusinessLogic.Utilities
{
    public static class IntegerExtensions
    {
        public static string ToSwedishString(this int value)
        {
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE"));
        }
        public static string ToSwedishString(this int value, string format)
        {
            return value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));
        }
    }
}
