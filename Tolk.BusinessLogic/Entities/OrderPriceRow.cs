using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderPriceRow : Utilities.PriceRowBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderPriceRowId { get; set; }

        public int OrderId { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

    }
}
