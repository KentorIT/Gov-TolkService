namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    public class OrderAgreementIdentifierModel
    {
        public int RequestId { get; set; }
        public int? RequisitionId { get; set; }

        public int CreateById => RequisitionId ?? RequestId;
    }
}