using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum OrderStatus
    {
        [Description("Sparat, ej skickat")]
        [Obsolete("Inte använd än")]
        Saved = 1,
        [Description("Uppdraget har skickats för tillsättning")]
        Requested = 2,
        [Description("Tolktillsättning finns")]
        RequestResponded = 3,
        [Description("Tolktillsättning har accepterats")]
        ResponseAccepted = 4,
        [Description("Uppdraget har genomförts")]
        Delivered = 5,
        [Description("Tolk har bekräftat tider")]
        Attested = 6,
        [Description("Leverans är bekräftad")]
        DeliveryAccepted = 7,
        [Description("Uppdraget har avbokats av avropare")]
        Cancelled = 8,
        [Description("Alla förmedlingar nekade uppdraget.")]
        NoBrokeracceptedOrder = 9
    }
}
