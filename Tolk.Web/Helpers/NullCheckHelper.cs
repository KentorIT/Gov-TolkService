using System;

namespace Tolk.Web.Helpers
{
    public static class NullCheckHelper
    {
        internal static void ArgumentCheckNull(object argumentToCheck, string className)
        {
            if (argumentToCheck == null)
            {
                throw new ArgumentNullException($"Argument is null in class {className}");
            }
        }

    }
}
