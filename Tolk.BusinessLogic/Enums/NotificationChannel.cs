using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum NotificationChannel
    {
        [Description("E-post")]
        Email = 1,
        [Description("Webhook")]
        Webhook = 2,
        [Description("Sfti")]
        Sfti = 3,
    }
}
