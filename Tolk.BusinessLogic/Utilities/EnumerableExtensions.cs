using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
