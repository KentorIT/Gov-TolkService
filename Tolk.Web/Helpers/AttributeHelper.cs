using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    public static class AttributeHelper
    {
        public static bool IsAttributeDefined(Type attributeType, Type objectType, string propertyName)
        {
            var property = objectType.GetProperty(propertyName);
            return Attribute.IsDefined(property, attributeType);
        }

        public static Attribute GetAttribute<T>(Type objectType, string propertyName)
        {
            var property = objectType.GetProperty(propertyName);
            return Attribute.GetCustomAttribute(property, typeof(T));
        }
    }
}
