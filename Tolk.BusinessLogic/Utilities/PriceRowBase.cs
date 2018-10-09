using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class PriceRowBase
    {
        public int? PriceListRowId { get; set; }

        public int? PriceCalculationChargeId { get; set; }

        public DateTimeOffset StartAt { get; set; }

        public DateTimeOffset EndAt { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public PriceRowType PriceRowType { get; set; }

        [ForeignKey(nameof(PriceListRowId))]
        public PriceListRow PriceListRow { get; set; }

        [ForeignKey(nameof(PriceCalculationChargeId))]
        public PriceCalculationCharge PriceCalculationCharge { get; set; }
    }
}
