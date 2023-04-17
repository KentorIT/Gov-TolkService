using System.Globalization;

namespace Tolk.BusinessLogic.Utilities
{
    public static class BooleanExtensions
    {
        public static string ToSwedishString(this bool value) => value ? "Ja" : "Nej";
    }
}
