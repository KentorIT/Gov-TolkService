using System;

namespace Tolk.Web.Helpers
{
    public static class TypeHelper
    {
        public static bool HasGenericTypeBase(Type type, Type genericType)
        {
            if (type != null)
            {
                while (type != typeof(object))
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                    {
                        return true;
                    }
                    type = type.BaseType;
                }
            }
            return false;
        }
    }
}
