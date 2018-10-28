using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum OrderStatus
    {
        /// <summary>
        /// This status is not used in db, just for filter lists
        /// </summary>
        [Description("Tolktillsättning finns/Tolktillsättning finns (Ny tolk)")]
        ToBeProcessedByCustomer = -1,
        [Description("Sparat, ej skickat")]
        [Obsolete("Inte använd än")]
        Saved = 1,
        [Description("Bokningsförfrågan skickad")]
        Requested = 2,
        [Description("Tolk är tillsatt")]
        RequestResponded = 3,
        [Description("Tillsättning är godkänd")]
        ResponseAccepted = 4,
        [Description("Uppdraget har genomförts")]
        Delivered = 5,
        [Description("Uppdrag avbokat av myndighet")]
        CancelledByCreator = 6,
        [Description("Leverans är bekräftad")]
        DeliveryAccepted = 7,
        [Description("Uppdraget har annullerats via reklamation")]
        [Obsolete("Inte använd än")]
        OrderAnulled = 8,
        [Description("Bokningsförfrågan avböjd av samtliga förmedlingar")]
        NoBrokerAcceptedOrder = 9,
        [Description("Tolk är tillsatt (Ny tolk)")]
        RequestRespondedNewInterpreter = 10,
        [Description("Avbokning bekräftad av förmedling")]
        CancelledByCreatorConfirmed = 11,
        [Description("Uppdrag avbokat av förmedling")]
        CancelledByBroker = 12,
        [Description("Avbokning bekräftad av myndighet")]
        CancelledByBrokerConfirmed = 13,
        [Description("Bokningsbekräftelse ej besvarad")]
        ResponseNotAnsweredByCreator = 15,
    }
}
