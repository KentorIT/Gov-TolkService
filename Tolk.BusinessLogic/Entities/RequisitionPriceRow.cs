using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class RequisitionPriceRow : PriceRowBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequisitionPriceRowId { get; set; }

        public int RequisitionId { get; set; }

        [ForeignKey(nameof(RequisitionId))]
        public Requisition Requisition { get; set; }
    }
}
