using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderHistoryEntryId { get; set; }

        public int OrderChangeLogEntryId { get; set; }

        public ChangeOrderType ChangeOrderType { get; set; }

        public string Value { get; set; }

        [ForeignKey(nameof(OrderChangeLogEntryId))]
        public OrderChangeLogEntry OrderChangeLogEntry { get; set; }
    }
}
