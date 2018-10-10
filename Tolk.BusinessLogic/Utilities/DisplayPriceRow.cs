
namespace Tolk.BusinessLogic.Utilities
{
    public class DisplayPriceRow
    {
        public string Description { get; set; }

        public string DescriptionToUse { get => HasSeparateSubTotal ? $"Summa {Description.ToLower()}" : Description; }

        public decimal Price { get; set; }

        public bool HasSeparateSubTotal { get; set; }

        public int DisplayOrder { get; set; }
    }
}
