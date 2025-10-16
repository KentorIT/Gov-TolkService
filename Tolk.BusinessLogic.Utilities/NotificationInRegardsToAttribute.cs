using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set what overall type notification is referencing
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotificationInRegardsToAttribute : Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        public NotificationInRegardsToAttribute(NotificationInRegardsToType notificationInRegardsToType)
        {
            NotificationInRegardsToType = notificationInRegardsToType;
        }

        /// <summary>
        /// Channel
        /// </summary>
        public NotificationInRegardsToType NotificationInRegardsToType { get; private set; }
    }
}
