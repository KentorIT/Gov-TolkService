using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public enum RequestStatus
    {
        Created = 1,
        Received = 2,
        SentToInterpreter = 3,
        Accepted = 4,
        Approved = 5,
        DeniedByInterpreter = 6,
        DeniedByBroker = 7,
        DeniedByCreator = 8,
        DeniedByTimeLimit = 9
    }
}
