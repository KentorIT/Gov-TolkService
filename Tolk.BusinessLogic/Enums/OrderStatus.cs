using System;
using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

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
        [Parent(NegotiationState.UnderNegotiation)]
        Requested = 2,
        [Description("Tolk är tillsatt, med resekostnader som behöver godkännas")]
        [Parent(NegotiationState.UnderNegotiation)]
        RequestRespondedAwaitingApproval = 3,
        [Description("Tillsättning är godkänd")]
        [Parent(NegotiationState.ContractValid)]
        ResponseAccepted = 4,
        [Description("Uppdrag har utförts")]
        [Parent(NegotiationState.ContractValid)]
        Delivered = 5,
        [Description("Uppdrag avbokat av myndighet")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        CancelledByCreator = 6,
        //this might be used when it is possible to approve requisitions
        [Description("Utförande bekräftat")]
        [Parent(NegotiationState.ContractValid)]
        DeliveryAccepted = 7,
        [Description("Uppdraget har annullerats via reklamation")]
        [Obsolete("Inte använd än")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        OrderAnulled = 8,
        [Description("Bokningsförfrågan avböjd av samtliga förmedlingar")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        NoBrokerAcceptedOrder = 9,
        [Description("Tolk är tillsatt (Ny tolk)")]
        [Parent(NegotiationState.ContractValid)]
        RequestRespondedNewInterpreter = 10,
        [Description("Uppdrag avbokat av förmedling")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        CancelledByBroker = 12,
        [Description("Tillsättning ej besvarad")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        ResponseNotAnsweredByCreator = 15,
        [Description("Sista svarstid ej satt")]
        [Parent(NegotiationState.UnderNegotiation)]
        AwaitingDeadlineFromCustomer = 16,
        [Description("Uppdrag annullerat, sista svarstid ej satt")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        NoDeadlineFromCustomer = 17,
        [Description("Tolk är tillsatt och väntar på godkännande i grupp med två tolkar")]
        [Parent(NegotiationState.UnderNegotiation)]
        RequestAwaitingPartialAccept = 18,
        [Description("Tillsättning är godkänd för ena tolken, andra har inte tillsatts än")]
        [Parent(NegotiationState.UnderNegotiation)]
        GroupAwaitingPartialResponse = 19,
        [Description("Bokningsförfrågan avbruten eftersom ramavtalet löpte ut")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        TerminatedDueToTerminatedFrameworkAgreement = 20,
        [Description("Förfrågan bekräftad av förmedling, inväntar tolktillsättning")]
        [Parent(NegotiationState.UnderNegotiation)]
        RequestRespondedAwaitingInterpreter = 21,

    }
}
