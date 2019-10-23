using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.Api.Payloads.Responses
{
    public class CustomerItemResponse : ListItemResponse
    {
        public string Name { get; set; }
        public string OrganisationNumber { get; set; }
        public string PriceListType { get; set; }
        public string TravelCostAgreementType { get; set; }
    }
}
