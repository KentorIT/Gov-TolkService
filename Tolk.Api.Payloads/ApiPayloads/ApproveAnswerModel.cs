using NJsonSchema.Annotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    public class ApproveAnswerModel : ApiPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string BrokerIdentifier { get; set; }
    }
}
