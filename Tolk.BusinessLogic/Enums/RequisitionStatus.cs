using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequisitionStatus
    {
        [CustomName("requisition_created")]
        [Description("Rekvisition är skapad")]
        [Parent(NegotiationState.UnderNegotiation)]
        Created = 1,
        [CustomName("requisition_reviewed")]
        [Description("Rekvisition är granskad")]
        [Parent(NegotiationState.ContractValid)]
        Reviewed = 2,
        [CustomName("requisition_commented")]
        [Description("Rekvisition har kommenterats")]
        [Parent(NegotiationState.UnderNegotiation)]
        Commented = 3,
        [CustomName("requisition_automatically_created")]
        [Description("Rekvisition är automatiskt genererad pga avbokning")]
        [Parent(NegotiationState.ContractValid)]
        AutomaticGeneratedFromCancelledOrder = 4,
        [CustomName("requisition_approved")]
        [Description("Rekvisition är godkänd")]
        [Parent(NegotiationState.ContractValid)]
        Approved = 5,
        [CustomName("requisition_denied")]
        [Description("Rekvisition är underkänd")]
        [Parent(NegotiationState.TerminatedPrematurely)]
        DeniedByCustomer = 6,
    }
}
