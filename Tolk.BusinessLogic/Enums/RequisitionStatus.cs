using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequisitionStatus
    {
        [Description("Skapad")]
        Created = 1,
        [Description("Godkänd")]
        Approved = 2,
        [Description("Underkänd")]
        DeniedByCustomer = 3,
    }
}
