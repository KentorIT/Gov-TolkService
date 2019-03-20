using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequisitionStatus
    {
        [CustomName("requisition_created")]
        [Description("Rekvisition är skapad")]
        Created = 1,
        [CustomName("requisition_reviewed")]
        [Description("Rekvisition är granskad")]
        Reviewed = 2,
        [CustomName("requisition_commented")]
        [Description("Rekvisition har kommenterats")]
        Commented = 3,
        [CustomName("requisition_automatically_created")]
        [Description("Rekvisition är automatiskt genererad pga avbokning")]
        AutomaticGeneratedFromCancelledOrder = 4,
        [CustomName("requisition_approved")]
        [Description("Rekvisition är godkänd")]
        Approved = 5,
        [CustomName("requisition_denied")]
        [Description("Rekvisition är underkänd")]
        DeniedByCustomer = 6,
    }
}
