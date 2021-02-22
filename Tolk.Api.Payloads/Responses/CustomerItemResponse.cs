using System.Collections.Generic;

namespace Tolk.Api.Payloads.Responses
{
    public class CustomerItemResponse : ListItemResponse
    {
        public string Name { get; set; }
        public string OrganisationNumber { get; set; }
        public string PriceListType { get; set; }
        public string TravelCostAgreementType { get; set; }
        public bool UseSelfInvoicingInterpreter { get; set; }
    }
    public class BrokerItemResponse : ListItemResponse
    {
        public string Name { get; set; }
        public string OrganisationNumber { get; set; }
        public IEnumerable<string> Regions { get; set; }
    }
}
