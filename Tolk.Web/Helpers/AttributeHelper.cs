using System;

namespace Tolk.Web.Helpers
{
    public static class AttributeHelper
    {
        public static bool IsAttributeDefined<T>(Type objectType, string propertyName)
        {
            var property = objectType?.GetProperty(propertyName);
            return Attribute.IsDefined(property, typeof(T));
        }

        public static Attribute GetAttribute<T>(Type objectType, string propertyName)
        {
            var property = objectType?.GetProperty(propertyName);
            return Attribute.GetCustomAttribute(property, typeof(T));
        }
    }
}
