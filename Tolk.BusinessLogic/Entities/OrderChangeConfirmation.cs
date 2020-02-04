using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities 
{
    public class OrderChangeConfirmation : StatusConfirmationBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderChangeConfirmationId { get; set; }

        public int OrderChangeLogEntryId { get; set; }

        [ForeignKey(nameof(OrderChangeLogEntryId))]
        public OrderChangeLogEntry OrderChangeLogEntry { get; set; }
    }
}
