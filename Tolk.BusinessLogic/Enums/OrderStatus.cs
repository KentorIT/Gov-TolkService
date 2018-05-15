using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum OrderStatus
    {
        [Description("Sparat, ej skickat")]
        Saved = 1,
        [Description("Uppdraget har skickats för tillsättning")]
        Requested = 2,
        [Description("Tolktillsättning finns")]
        RequestResponded = 3,
        [Description("Uppdraget tolktillsättning har accepterats")]
        ResponseAccepted = 4,
        [Description("Uppdraget har genomförts")]
        Delivered = 5,
        [Description("Tolk har bekräftat tider")]
        Attested = 6,
        [Description("Leverans är bekräftad")]
        DeliveryAccepted = 7,
    }
}
