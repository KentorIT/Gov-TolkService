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
            return typeof(TEnum).GetMember(value.ToString()).GetEnumDescription() ?? value.ToString();
        }

        /// <summary>
        /// Gets the shorter version of descriptive text of an enum value.
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum.</typeparam>
        /// <param name="value">Enum value.</param>
        /// <returns>Short description string.</returns>
        public static string GetShortDescription<TEnum>(TEnum value) where TEnum : struct
        {
            return GetAttributeProperty<ShortDescriptionAttribute, TEnum>(value)?.ShortDescription ?? value.ToString();
        }

        /// <summary>
        /// Gets the custom name
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum.</typeparam>
        /// <param name="value">Enum value.</param>
        /// <returns>custom name if possible, otherwize the enum as a string.</returns>
        public static string GetCustomName<TEnum>(TEnum value) where TEnum : struct
        {
            return GetAttributeProperty<CustomNameAttribute, TEnum>(value)?.CustomName ?? value.ToString();
        }
        public static double GetVat<TEnum>(this TEnum value) where TEnum : struct
        {
            return GetAttributeProperty<VatAttribute, TEnum>(value)?.Vat ?? 0;
        }

        public static TEnum? GetEnumByCustomName<TEnum>(string value) where TEnum : struct
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            return Enum.GetValues(type).OfType<TEnum>()
                .SingleOrDefault(v => GetAttributeProperty<CustomNameAttribute, TEnum>(v)?.CustomName == value);
        }
        public static string GetTellusName<TEnum>(TEnum value) where TEnum : struct
        {
            return GetAttributeProperty<TellusNameAttribute, TEnum>(value)?.Name ?? value.ToString();
        }
        public static TEnum? GetEnumByTellusName<TEnum>(string value) where TEnum : struct
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            return Enum.GetValues(type).OfType<TEnum>()
                .SingleOrDefault(v => GetAttributeProperty<TellusNameAttribute, TEnum>(v)?.Name == value);
        }

        public static IEnumerable<NotificationChannel> GetAvailableNotificationChannels<TEnum>(TEnum value) where TEnum : struct
        {
            return GetAttributeProperties<AvailableNotificationChannelAttribute, TEnum>(value).Select(t => t.NotificationChannel);
        }

        public static IEnumerable<NotificationConsumerType> GetAvailableNotificationConsumerTypes<TEnum>(TEnum value) where TEnum : struct
        {
            return GetAttributeProperties<NotificationConsumerTypeAttribute, TEnum>(value).Select(n => n.NotificationConsumerType);
        }

        /// <summary>
        /// Returns the set parent of type TEnumParent
        /// </summary>
        public static TEnumParent Parent<TEnum, TEnumParent>(TEnum value)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            var attributes = type.GetMember(value.ToString()).Single().GetCustomAttributes(false);

            var property = attributes.OfType<ParentAttribute>().Where(a => a.Parent is TEnumParent).SingleOrDefault();
            return property != null ? (TEnumParent)property.Parent : default;
        }

        /// <summary>
        /// Used to determine if a enum value is marked as obsolete.
        /// </summary>
        public static bool IsObsolete<TEnum>(TEnum value)
        {
            return (value == null || GetAttributeProperty<ObsoleteAttribute, TEnum>(value) != null);
        }

        public static bool UseInApi<TEnum>(TEnum value)
        {
            return (value != null && (GetAttributeProperty<CustomNameAttribute, TEnum>(value)?.UseInApi ?? false));
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
        public static IEnumerable<TEnum> GetEnumsWithParent<TEnum, TParentEnum>(TParentEnum parent)
            where TParentEnum : struct
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            return Enum.GetValues(type).OfType<TEnum>().Where(t => parent.Equals(Parent<TEnum, TParentEnum>(t)));
        }

        public static IEnumerable<EnumDescription<TEnum>> GetAllFullDescriptions<TEnum>(IEnumerable<TEnum> filterValues = null, bool onlyApiDescriptions = true)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;

            return Enum.GetValues(type).OfType<TEnum>()
                .Where(t => !IsObsolete(t) && (UseInApi(t) || !onlyApiDescriptions) &&
                    (filterValues == null || filterValues.Contains(t)))
                .Select(v => new EnumDescription<TEnum>(v,
                    type.GetMember(v.ToString()).GetEnumDescription(),
                    type.GetMember(v.ToString()).GetEnumCustomName()));
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

        public static IEnumerable<TAttribute> GetAttributeProperties<TAttribute, TEnum>(TEnum value)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.GetMember(value.ToString())
                .Single()
                .GetCustomAttributes(false)
                .OfType<TAttribute>();
        }

        private static string GetEnumDescription(this IEnumerable<MemberInfo> member)
        {
            var attributes = member.Single().GetCustomAttributes(false);
            var property = attributes.OfType<DescriptionAttribute>().SingleOrDefault();
            return property?.Description;
        }

        private static string GetEnumCustomName(this IEnumerable<MemberInfo> member)
        {
            var attributes = member.Single().GetCustomAttributes(false);
            var property = attributes.OfType<CustomNameAttribute>().SingleOrDefault();
            return property?.CustomName ?? string.Empty;
        }

        public static IEnumerable<TEnum> GetBiggerOrEqual<TEnum>(TEnum minimumLevel)
        {
            var type = typeof(TEnum);
            type = Nullable.GetUnderlyingType(type) ?? type;
            return Enum.GetValues(type).OfType<TEnum>().Where(t => Comparer<TEnum>.Default.Compare(t, minimumLevel) >= 0);
        }

        public static T Parse<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static ContractDefinitionAttribute GetContractDefinition<TEnum>(TEnum value)
        {
            return GetAttributeProperty<ContractDefinitionAttribute, TEnum> (value);            
        }
    }
}
