using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class CreateOrderResponse : ResponseBase
    {
        public string OrderNumber { get; set; }
        public PriceInformationModel PriceInformation { get; set; }
    }
}
