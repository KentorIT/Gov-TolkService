using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set a name uesd in for example webhooks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CustomNameAttribute : Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        public CustomNameAttribute(string customName)
        {
            CustomName = customName;
        }

        /// <summary>
        /// The parent
        /// </summary>
        public string CustomName { get; private set; }
    }
}
