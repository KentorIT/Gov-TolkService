using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequisitionStatusConfirmation : StatusConfirmationBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequisitionStatusConfirmationId { get; set; }

        public int RequisitionId { get; set; }

        [ForeignKey(nameof(RequisitionId))]
        public Requisition Requisition { get; set; }

        public RequisitionStatus RequisitionStatus { get; set; }
    }
}
