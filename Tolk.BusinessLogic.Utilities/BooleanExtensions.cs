using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
