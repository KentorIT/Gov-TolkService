using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;
namespace Tolk.BusinessLogic.Entities
{
    public class OrderChangeLogEntry
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderChangeLogEntryId { get; set; }

        public OrderChangeLogType OrderChangeLogType { get; set; }

        public int OrderId { get; set; }

        public int? UpdatedByUserId { get; set; }

        public int? UpdatedByImpersonatorId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public Order Order { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public AspNetUser UpdatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedByImpersonatorId))]
        public AspNetUser UpdatedByImpersonatorUser { get; set; }

        public List<OrderHistoryEntry> OrderHistories { get; set; }

        public OrderContactPersonHistory OrderContactPersonHistory { get; set; }
    }
}
