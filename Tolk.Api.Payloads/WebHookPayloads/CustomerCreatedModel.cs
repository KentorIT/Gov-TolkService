namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class CustomerCreatedModel : WebHookPayloadBaseModel
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string OrganisationNumber { get; set; }
        public string PriceListType { get; set; }
        public string TravelCostAgreementType { get; set; }
        public bool UseSelfInvoicingInterpreter { get; set; }
    }
}
