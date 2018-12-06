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
        public CustomNameAttribute(string customName, bool useInApi = true)
        {
            CustomName = customName;
            UseInApi = useInApi;
        }

        /// <summary>
        /// The parent
        /// </summary>
        public string CustomName { get; private set; }

        public bool UseInApi { get; private set; }
    }
}
