using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    public static class TypeHelper
    {
        public static bool HasGenericTypeBase(Type type, Type genericType)
        {
            while (type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
                type = type.BaseType;
            }

            return false;
        }
    }
}
