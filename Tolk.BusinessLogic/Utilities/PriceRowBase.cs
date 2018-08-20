using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Entities;


namespace Tolk.BusinessLogic.Utilities
{
    public class PriceRowBase
    {
        public int PriceListRowId { get; set; }

        public DateTimeOffset StartAt { get; set; }

        public DateTimeOffset EndAt { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalPrice { get; set; }

        public bool IsBrokerFee { get; set; } = false;

        [ForeignKey(nameof(PriceListRowId))]
        public PriceListRow PriceListRow { get; set; }
    }
}
