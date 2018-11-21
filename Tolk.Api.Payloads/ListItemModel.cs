namespace Tolk.Api.Payloads
{
    public class ListItemModel
    {
        public string Key { get; set; }

        public string Description { get; set; }

    }
    public class CustomerItemModel : ListItemModel
    {
        public string PriceListType { get; set; }

    }
}
