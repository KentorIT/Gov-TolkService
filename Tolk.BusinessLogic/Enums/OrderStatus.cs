using System;
using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum OrderStatus
    {
        /// <summary>
        /// This status is not used in db, just for filter lists
        /// </summary>
        [Description("Tolk är tillsatt/Tolk är tillsatt (Ny tolk)")]
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
        [Description("Uppdrag har utförts")]
        Delivered = 5,
        [Description("Uppdrag avbokat av myndighet")]
        CancelledByCreator = 6,
        //this might be used when it is possible to approve requisitions
        [Description("Utförande bekräftat")]
        DeliveryAccepted = 7,
        [Description("Uppdraget har annullerats via reklamation")]
        [Obsolete("Inte använd än")]
        OrderAnulled = 8,
        [Description("Bokningsförfrågan avböjd av samtliga förmedlingar")]
        NoBrokerAcceptedOrder = 9,
        [Description("Tolk är tillsatt (Ny tolk)")]
        RequestRespondedNewInterpreter = 10,
        [Description("Uppdrag avbokat av förmedling")]
        CancelledByBroker = 12,
        [Description("Bokningsbekräftelse ej besvarad")]
        ResponseNotAnsweredByCreator = 15,
        [Description("Sista svarstid ej satt")]
        AwaitingDeadlineFromCustomer = 16,
        [Description("Uppdrag avbokat, sista svarstid ej satt")]
        NoDeadlineFromCustomer = 17,
        [Description("Tolk är tillsatt och väntar på godkännande i grupp med två tolkar")]
        RequestAwaitingPartialAccept = 18,
        [Description("Tillsättning är godkänd för ena tolken, andra har inte tillsatts än")]
        GroupAwaitingPartialResponse = 19,
    }
}
