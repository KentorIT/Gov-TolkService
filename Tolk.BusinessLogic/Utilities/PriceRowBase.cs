using System;
using System.Linq;
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

        public int Minutes { get => (int)(EndAt - StartAt).TotalMinutes; }

        public decimal TotalPrice { get => Price * Quantity; }

        public decimal RoundedTotalPrice { get => RoundedPrice * Quantity; }

        private decimal RoundedPrice { get => decimal.Round(Price, 2, MidpointRounding.AwayFromZero); }

        public decimal Decimals { get => Quantity * (RoundedPrice - Math.Floor(RoundedPrice)); }

    }
}
