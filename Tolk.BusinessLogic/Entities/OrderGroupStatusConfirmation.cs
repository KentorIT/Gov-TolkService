using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderGroupStatusConfirmation : StatusConfirmationBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderGroupStatusConfirmationId { get; set; }

        public int OrderGroupId { get; set; }

        [ForeignKey(nameof(OrderGroupId))]
        public OrderGroup OrderGroup { get; set; }

        public OrderStatus OrderStatus { get; set; }
    }
}
