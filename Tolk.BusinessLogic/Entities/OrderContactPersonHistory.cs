using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderContactPersonHistory
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderContactPersonHistoryId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public int? PreviousContactPersonId { get; set; }

        [ForeignKey(nameof(PreviousContactPersonId))]
        public AspNetUser PreviousContactPersonUser { get; set; }

        public DateTimeOffset ChangedAt { get; set; }

        public int ChangedBy { get; set; }

        [ForeignKey(nameof(ChangedBy))]
        public AspNetUser ChangedByUser { get; set; }

        public int? ImpersonatingChangeUserId { get; set; }

        [ForeignKey(nameof(ImpersonatingChangeUserId))]
        public AspNetUser ChangedByImpersonator { get; set; }
    }
}
