using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequisitionStatus
    {
        [Description("Rekvisition är skapad")]
        Created = 1,
        [Description("Rekvisition är godkänd")]
        Approved = 2,
        [Description("Rekvisition är underkänd")]
        DeniedByCustomer = 3,
        [Description("Rekvisition är automatiskt godkänd pga sen avbokning")]
        AutomaticApprovalFromCancelledOrder = 4,
    }
}
