namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class CustomerInformationModel : CustomerInformationBaseModel
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string OrganisationNumber { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string PriceListType { get; set; }
        public string TravelCostAgreementType { get; set; }
    }
}
