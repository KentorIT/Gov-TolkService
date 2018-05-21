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
        /// <summary>
        /// Returns the set parent of type TEnumParent
        /// </summary>
        public static TEnumParent Parent<TEnum, TEnumParent>(TEnum value)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            var attributes = type.GetMember(value.ToString()).Single().GetCustomAttributes(false);

            var property = attributes.OfType<ParentAttribute>().SingleOrDefault();
            if (property != null)
            {
                return (TEnumParent)property.Parent;
            }
            else
            {
                return default(TEnumParent);
            }
        }

        /// <summary>
        /// Used to determine if a enum value is marked as obsolete.
        /// </summary>
        public static bool IsObsolete<TEnum>(TEnum value)
        {
            return (value == null ? true : GetAttributeProperty<ObsoleteAttribute, TEnum>(value) != null);
        }

        /// <summary>
        /// Get all description values for an enum.
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum.</typeparam>
        /// <returns>IEnumerable of enum descriptions.</returns>
        public static IEnumerable<EnumValue<TEnum>> GetAllDescriptions<TEnum>(IEnumerable<TEnum> filterValues = null)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            return Enum.GetValues(type).OfType<TEnum>()
                .Where(t => !IsObsolete(t) && (filterValues == null || filterValues.Contains(t)))
                .Select(v => new EnumValue<TEnum>(v, type.GetMember(v.ToString()).GetEnumDescription()));
        }

        /// <summary>
        /// returns the attribute of the TAttribute typ, if the incoming field has it set.
        /// </summary>
        public static TAttribute GetAttributeProperty<TAttribute, TEnum>(TEnum value)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;
            var property = type.GetMember(value.ToString())
                .Single()
                .GetCustomAttributes(false)
                .OfType<TAttribute>().SingleOrDefault();
            return property;
        }

        private static string GetEnumDescription(this IEnumerable<MemberInfo> member)
        {
            var attributes = member.Single().GetCustomAttributes(false);
            var property = attributes.OfType<DescriptionAttribute>().SingleOrDefault();
            return property?.Description;
        }
    }
}
