using System;

namespace Tolk.BusinessLogic.Utilities
{
    public class DisplayPriceRow
    {
        public string Description { get; set; }

        public string DescriptionToUse { get => HasSeparateSubTotal ? $"Summa {Description.ToLower()}" : Description; }

        public decimal Price { get; set; }

        public decimal RoundedPrice { get => decimal.Round(Price, 2, MidpointRounding.AwayFromZero); }

        public bool HasSeparateSubTotal { get; set; }

        public int DisplayOrder { get; set; }
    }
}
