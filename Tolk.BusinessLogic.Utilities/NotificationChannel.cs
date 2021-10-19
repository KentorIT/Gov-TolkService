using System.ComponentModel;

namespace Tolk.BusinessLogic.Utilities
{
    public enum NotificationChannel
    {
        [Description("E-post")]
        Email = 1,
        [Description("Webhook")]
        Webhook = 2,
        [Description("Peppol")]
        Peppol = 3,
        //Startpage, to be able to set if you want a specific notification type to appear on the startpage.

    }
}
