using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set other sort order on enum fields, if the numbering is not to be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ParentAttribute : Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        public ParentAttribute(object parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// The parent
        /// </summary>
        public object Parent { get; private set; }
    }
}
