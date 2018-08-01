﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
        [Description("Svar skickat")]
        Accepted = 4,
        [Description("Tillsättning godkänd")]
        Approved = 5,
        [Description("Nekad av förmedling")]
        DeclinedByBroker = 7,
        [Description("Nekad av avropare")]
        DeniedByCreator = 8,
        [Description("Inget svar, tiden gick ut")]
        DeniedByTimeLimit = 9
    }
}
