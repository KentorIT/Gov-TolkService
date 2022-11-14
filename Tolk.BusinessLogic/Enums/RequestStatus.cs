using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequestStatus
    {
        /// <summary>
        /// This status is not used in db, just for filter lists
        /// </summary>
        [CustomName("special", false)]
        [Description("Inkommen/mottagen")]
        ToBeProcessedByBroker = -1,
        [CustomName("request_created")]
        [Description("Bokningsförfrågan inkommen")]
        [Parent(NegotiationState.UnderNegotiation)]
        Created = 1,
        [CustomName("request_received")]
        [Description("Bokningsförfrågan mottagen")]
        [Parent(NegotiationState.UnderNegotiation)]
        Received = 2,
        [CustomName("request_cancelled_by_creator")]
        [Description("Avbokad av myndighet")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        CancelledByCreator = 3,
        [CustomName("request_answer_awaiting_approval")]
        [Description("Bekräftelse är skickad")]
        [Parent(NegotiationState.UnderNegotiation)]
        AcceptedAwaitingApproval = 4,
        [CustomName("request_answer_approved")]
        [Description("Tillsättning är godkänd")]
        [Parent(NegotiationState.ContractValid)]
        Approved = 5,
        [CustomName("request_delivered")]
        [Description("Uppdrag har utförts")]
        [Parent(NegotiationState.ContractValid)]
        Delivered = 6,
        [CustomName("request_declined")]
        [Description("Bokningsförfrågan avböjd")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        DeclinedByBroker = 7,
        [CustomName("request_answer_denied")]
        [Description("Tillsättning är avböjd")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        DeniedByCreator = 8,
        [CustomName("request_answer_denied_by_time_limit")]
        [Description("Bokningsförfrågan ej besvarad")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        DeniedByTimeLimit = 9,
        [CustomName("request_cancelled_by_creator_when_approved")]
        [Description("Uppdrag avbokat av myndighet")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        CancelledByCreatorWhenApproved = 10,
        [CustomName("request_new_interpreter_needs_approval")]
        [Description("Bekräftelse är skickad - Ny tolk")]
        [Parent(NegotiationState.ContractValid)]
        AcceptedNewInterpreterAppointed = 12,
        [CustomName("request_replaced_interpreter")]
        [Description("Tolk har ersatts")]
        [Parent(NegotiationState.ReplacedByOtherEntity)]
        InterpreterReplaced = 13,
        [CustomName("request_cancelled_by_broker")]
        [Description("Uppdrag avbokat av förmedling")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        CancelledByBroker = 14,
        [CustomName("request_answer_never_processed_by_creator")]
        [Description("Tillsättning ej besvarad")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        ResponseNotAnsweredByCreator = 16,
        [Description("Inväntar sista svarstid från myndighet")]
        [CustomName("not_used", false)]
        [Parent(NegotiationState.UnderNegotiation)]
        AwaitingDeadlineFromCustomer = 17,
        [Description("Ingen sista svarstid från myndighet")]
        [CustomName("request_no_deadline_from_customer")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        NoDeadlineFromCustomer = 18,
        [CustomName("not_used", false)]
        [Description("Förlorad på grund av karantän")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        LostDueToQuarantine = 19,
        [CustomName("not_used", false)]
        [Description("Gruppförfrågan med extra tolk har en tolk tillsatt med resekostnader, och en tolk ännu ej tillsatt")]
        [Parent(NegotiationState.UnderNegotiation)]
        PartiallyAccepted = 20,
        [CustomName("not_used", false)]
        [Description("Gruppförfrågan med extra tolk har tillsatt och godkänd tolk, och en tolk ännu ej tillsatt")]
        [Parent(NegotiationState.UnderNegotiation)]
        PartiallyApproved = 21,
        [CustomName("terminated_due_to_terminated_framework_agreement")]
        [Description("Förfrågan avbruten eftersom ramavtalet löpte ut")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        TerminatedDueToTerminatedFrameworkAgreement = 22,
        [CustomName("request_answer_awaiting_interpreter")]
        [Description("Förfrågan bekräftad av förmedling, inväntar tolktillsättning")]
        [Parent(NegotiationState.UnderNegotiation)]
        AcceptedAwaitingInterpreter = 23,
    }
}
