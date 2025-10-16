using System.ComponentModel;

namespace Tolk.BusinessLogic.Utilities
{
    public enum NotificationInRegardsToType
    {
        [Description("Bokning")]
        Order = 1,
        [Description("Förfrågan")]
        Request = 2,
    }
}
