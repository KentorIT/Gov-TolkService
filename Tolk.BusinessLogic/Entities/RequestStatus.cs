using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public enum RequestStatus
    {
        [Description("Inkommen")]
        Created = 1,
        [Description("Mottagen")]
        Received = 2,
        [Description("Hos tolk")]
        SentToInterpreter = 3,
        [Description("Svar skickat")]
        Accepted = 4,
        [Description("Tillsättning godkänd")]
        Approved = 5,
        [Description("Nekad av tolk")]
        DeniedByInterpreter = 6,
        [Description("Nekad av förmedling")]
        DeniedByBroker = 7,
        [Description("Nekad av avropare")]
        DeniedByCreator = 8,
        [Description("Inget svar, tiden gick ut")]
        DeniedByTimeLimit = 9
    }
}
