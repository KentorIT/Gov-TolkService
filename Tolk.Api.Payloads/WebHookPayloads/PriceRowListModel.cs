namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class PriceRowListModel
    {
        public string PriceListRowType { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice
        {
            get
            {
                return Price * Quantity;
            }
        }
    }
}
