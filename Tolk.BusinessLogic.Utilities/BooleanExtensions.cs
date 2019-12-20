using System.Globalization;

namespace Tolk.BusinessLogic.Utilities
{
    public static class BooleanExtensions
    {
        public static string ToSwedishString(this bool value)
        {
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE"));
        }
    }
}
