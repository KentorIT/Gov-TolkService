using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Tolk.BusinessLogic.Utilities
{
    public static class EnumHelper
    {

        public static string GetDescription(Type enumType, Enum value)
        {
            enumType = Nullable.GetUnderlyingType(enumType) ?? enumType;

            return value == null ? string.Empty : enumType.GetMember(value.ToString()).GetEnumDescription();
        }
            /// <summary>
            /// Gets the descriptive text of a nullable enum value.
            /// </summary>
            /// <typeparam name="TEnum">Type of the enum.</typeparam>
            /// <param name="value">Enum value.</param>
            /// <returns>Description string.</returns>
            public static string GetDescription<TEnum>(TEnum? value) where TEnum : struct
        {
            if (value.HasValue)
            {
                return GetDescription(value.Value);
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the descriptive text of an enum value.
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum.</typeparam>
        /// <param name="value">Enum value.</param>
        /// <returns>Description string.</returns>
        public static string GetDescription<TEnum>(TEnum value) where TEnum : struct
        {
            string description = typeof(TEnum).GetMember(value.ToString()).GetEnumDescription();
            if (description == null)
            {
                description = value.ToString();
            }
            return description;
        }

        private static string GetEnumDescription(this IEnumerable<MemberInfo> member)
        {
            var attributes = member.Single().GetCustomAttributes(false);
            var property = attributes.OfType<DescriptionAttribute>().SingleOrDefault();
            return property?.Description;
        }
    }
}
