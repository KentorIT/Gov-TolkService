using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequisitionStatus
    {
        [Description("Rekvisition är skapad")]
        Created = 1,
        [Description("Rekvisition är granskad")]
        Reviewed = 2,
        [Description("Rekvisition har kommenterats")]
        Commented = 3,
        [Description("Rekvisition är automatiskt genererad pga sen avbokning")]
        AutomaticGeneratedFromCancelledOrder = 4,
        [Description("Rekvisition är godkänd")]
        Approved = 5,
        [Description("Rekvisition är underkänd")]
        DeniedByCustomer = 6,
    }
}
