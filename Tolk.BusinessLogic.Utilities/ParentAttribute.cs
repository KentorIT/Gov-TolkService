﻿using System;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set other sort order on enum fields, if the numbering is not to be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
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
