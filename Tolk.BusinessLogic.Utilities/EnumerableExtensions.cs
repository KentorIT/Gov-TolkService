﻿using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Extension methods for IEnumerable - linq extensions.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Concat a single item onto a sequence.
        /// </summary>
        /// <typeparam name="T">Type of the item</typeparam>
        /// <param name="source">Existing sequence</param>
        /// <param name="item">Item to add</param>
        /// <returns>New sequence</returns>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
        {
            return source.Concat(item.WrapInEnumerable());
        }

        /// <summary>
        /// Create an enumerable with a single item in.
        /// </summary>
        /// <typeparam name="T">Type of the item.</typeparam>
        /// <param name="item">The item</param>
        /// <returns>Sequence with <paramref name="item"/> as the only content</returns>
        public static IEnumerable<T> WrapInEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static string GetDescription<T>(this T item) where T : struct
        {
            return EnumHelper.GetDescription<T>(item);
        }

        public static string GetShortDescription<T>(this T item) where T : struct
        {
            return EnumHelper.GetShortDescription<T>(item);
        }

        public static string GetCustomName<T>(this T item) where T : struct
        {
            return EnumHelper.GetCustomName<T>(item);
        }
        public static string GetTellusName<T>(this T item) where T : struct
        {
            return EnumHelper.GetTellusName<T>(item);
        }
        public static IEnumerable<NotificationChannel> GetAvailableNotificationChannels<T>(this T item) where T : struct
        {
            return EnumHelper.GetAvailableNotificationChannels<T>(item);
        }
        public static IEnumerable<NotificationConsumerType> GetAvailableNotificationConsumerTypes<T>(this T item) where T : struct
        {
            return EnumHelper.GetAvailableNotificationConsumerTypes<T>(item);
        }

        public static ContractDefinitionAttribute GetContractDefinitionAttribute<T>(this T item) where T : struct
        {
            return EnumHelper.GetContractDefinition<T>(item);
        }
    }
}
