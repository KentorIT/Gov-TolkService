using System.Globalization;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public static class IntegerExtensions
    {
        public static string ToSwedishString(this int value)
        {
            //'\u2212' is a "long" minus sign, that needs to be replaced with the ascii version, to be able to use it everywhere
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE")).Replace('\u2212', '-');
        }
        public static string ToSwedishString(this int value, string format)
        {
            //'\u2212' is a "long" minus sign, that needs to be replaced with the ascii version, to be able to use it everywhere
            return value.ToString(format, CultureInfo.GetCultureInfo("sv-SE")).Replace('\u2212', '-');
        }
    }
}
