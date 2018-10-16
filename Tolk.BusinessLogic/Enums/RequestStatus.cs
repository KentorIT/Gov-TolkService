using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequestStatus
    {
        /// <summary>
        /// This status is not used in db, just for filter lists
        /// </summary>
        [Description("Inkommen/mottagen")]
        ToBeProcessedByBroker = -1,
        [Description("Bokningsförfrågan inkommen")]
        Created = 1,
        [Description("Bokningsförfrågan mottagen")]
        Received = 2,
        [Description("Avbokad av myndighet")]
        CancelledByCreator = 3,
        [Description("Bekräftelse är skickad")]
        Accepted = 4,
        [Description("Tillsättning är godkänd")]
        Approved = 5,
        [Description("Bokningsförfrågan avböjd")]
        DeclinedByBroker = 7,
        [Description("Tillsättning är underkänd")]
        DeniedByCreator = 8,
        [Description("Bokningsförfrågan ej besvarad")]
        DeniedByTimeLimit = 9,
        [Description("Uppdrag avbokad av myndighet, att bekräfta")]
        CancelledByCreatorWhenApproved = 10,
        [Description("Avbokning bekräftad av förmedling")]
        CancelledByCreatorConfirmed = 11,
        [Description("Bekräftelse är skickad - Ny tolk")]
        AcceptedNewInterpreterAppointed = 12,
        [Description("Tolk har ersatts")]
        InterpreterReplaced = 13,
        [Description("Uppdrag avbokad av förmedling")]
        CancelledByBroker = 14,
        [Description("Avbokning bekräftad av myndighet")]
        CancelledByBrokerConfirmed = 15,
        [Description("Tillsättning ej besvarad")]
        ResponseNotAnsweredByCreator = 16
    }
}
