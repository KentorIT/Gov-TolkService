using System;

namespace Tolk.BusinessLogic.Helpers
{
    internal static class NullCheckHelper
    {
        internal static void ArgumentCheckNull(object argumentToTest, string methodName, string className)
        {
            if (argumentToTest == null)
            {
                throw new ArgumentNullException($"Argument is null in class {className}, method {methodName}");
            }
        }
    }
}
