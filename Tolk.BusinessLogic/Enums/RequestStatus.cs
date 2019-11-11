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
        Created = 1,
        [CustomName("request_received")]
        [Description("Bokningsförfrågan mottagen")]
        Received = 2,
        [CustomName("request_cancelled_by_creator")]
        [Description("Avbokad av myndighet")]
        CancelledByCreator = 3,
        [CustomName("request_answer_awaiting_approval")]
        [Description("Bekräftelse är skickad")]
        Accepted = 4,
        [CustomName("request_answer_approved")]
        [Description("Tillsättning är godkänd")]
        Approved = 5,
        [CustomName("request_declined")]
        [Description("Bokningsförfrågan avböjd")]
        DeclinedByBroker = 7,
        [CustomName("request_answer_denied")]
        [Description("Tillsättning är avböjd")]
        DeniedByCreator = 8,
        [CustomName("request_answer_denied_by_time_limit")]
        [Description("Bokningsförfrågan ej besvarad")]
        DeniedByTimeLimit = 9,
        [CustomName("request_cancelled_by_creator_when_approved")]
        [Description("Uppdrag avbokat av myndighet")]
        CancelledByCreatorWhenApproved = 10,
        [CustomName("request_new_interpreter_needs_approval")]
        [Description("Bekräftelse är skickad - Ny tolk")]
        AcceptedNewInterpreterAppointed = 12,
        [CustomName("request_replaced_interpreter")]
        [Description("Tolk har ersatts")]
        InterpreterReplaced = 13,
        [CustomName("request_cancelled_by_broker")]
        [Description("Uppdrag avbokat av förmedling")]
        CancelledByBroker = 14,
        [CustomName("request_answer_never_processed_by_creator")]
        [Description("Tillsättning ej besvarad")]
        ResponseNotAnsweredByCreator = 16,
        [Description("Inväntar sista svarstid från myndighet")]
        [CustomName("not_used", false)]
        AwaitingDeadlineFromCustomer = 17,
        [Description("Ingen sista svarstid från myndighet")]
        NoDeadlineFromCustomer = 18,
        [CustomName("not_used", false)]
        [Description("Förlorad på grund av karantän")]
        LostDueToQuarantine = 19,
        [CustomName("not_used", false)]
        [Description("Gruppförfrågan med extra tolk har en tolk tillsatt med resekostnader, och en tolk ännu ej tillsatt")]
        PartiallyAccepted = 20,
        [CustomName("not_used", false)]
        [Description("Gruppförfrågan med extra tolk har tillsatt och godkänd tolk, och en tolk ännu ej tillsatt")]
        PartiallyApproved = 21,
    }
}
