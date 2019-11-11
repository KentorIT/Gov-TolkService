using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderStatusConfirmation : StatusConfirmationBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderStatusConfirmationId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public OrderStatus OrderStatus { get; set; }
    }
}
