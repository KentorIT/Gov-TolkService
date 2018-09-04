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
        [Description("Inkommen")]
        Created = 1,
        [Description("Mottagen")]
        Received = 2,
        [Description("Avbokad av avropare")]
        CancelledByCreator = 3,
        [Description("Svar skickat")]
        Accepted = 4,
        [Description("Tillsättning godkänd")]
        Approved = 5,
        [Description("Nekad av förmedling")]
        DeclinedByBroker = 7,
        [Description("Nekad av avropare")]
        DeniedByCreator = 8,
        [Description("Inget svar, tiden gick ut")]
        DeniedByTimeLimit = 9,
        [Description("Avbokad av avropare, att bekräfta")]
        CancelledByCreatorWhenApproved = 10,
        [Description("Avbokning bekräftad av förmedling")]
        CancelledByCreatorConfirmed = 11,
        [Description("Svar skickat - Ny tolk")]
        AcceptedNewInterpreterAppointed = 12,
        [Description("Tolk har ersatts")]
        InterpreterReplaced = 13,
        [Description("Avbokad av tolkförmedling")]
        CancelledByBroker = 14,
        [Description("Avbokning bekräftad av avropare")]
        CancelledByBrokerConfirmed = 15
    }
}
