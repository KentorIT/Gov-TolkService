using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderStatusConfirmation
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderStatusConfirmationId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public OrderStatus OrderStatus { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public int? ConfirmedBy { get; set; }

        [ForeignKey(nameof(ConfirmedBy))]
        public AspNetUser ConfirmedByUser { get; set; }

        public int? ImpersonatingConfirmedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingConfirmedBy))]
        public AspNetUser ImpersonatingConfirmedByUser { get; set; }
    }
}
