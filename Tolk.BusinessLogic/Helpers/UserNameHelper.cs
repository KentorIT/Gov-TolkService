using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Tolk.BusinessLogic.Helpers
{
    public static class UserNameHelper
    {
        public static string GetPrefix(this string original, int length = 3)
        {
            return Regex.Replace(original, "[^0-9a-zA-Z_\\.\\-@]", "X").PadRight(length, 'x').Substring(0, length);
        }
    }
}
