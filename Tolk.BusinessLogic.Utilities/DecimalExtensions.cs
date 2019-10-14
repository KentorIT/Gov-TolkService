﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public static class DecimalExtensions
    {
        public static string ToSwedishString(this decimal value)
        {
            return value.ToString(CultureInfo.GetCultureInfo("sv-SE"));
        }
        public static string ToSwedishString(this decimal value, string format)
        {
            return value.ToString(format, CultureInfo.GetCultureInfo("sv-SE"));
        }
    }
}
