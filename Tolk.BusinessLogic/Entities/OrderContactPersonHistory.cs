using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderContactPersonHistory
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderContactPersonHistoryId { get; set; }

        public int? PreviousContactPersonId { get; set; }

        [ForeignKey(nameof(PreviousContactPersonId))]
        public AspNetUser PreviousContactPersonUser { get; set; }

        public int OrderChangeLogEntryId { get; set; }

        [ForeignKey(nameof(OrderChangeLogEntryId))]
        public OrderChangeLogEntry OrderChangeLogEntry { get; set; }

    }
}
