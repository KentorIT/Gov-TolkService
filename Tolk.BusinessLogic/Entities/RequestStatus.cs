using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public enum RequestStatus
    {
        Created = 1,
        Received = 2,
        Accepted = 3,
        DeniedByBroker = 4,
        DeniedByCreator = 5,
        DeniedByTimeLimit = 6
    }
}
