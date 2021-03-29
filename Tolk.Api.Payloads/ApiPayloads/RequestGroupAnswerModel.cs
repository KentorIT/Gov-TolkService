using NJsonSchema.Annotations;
using System;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    public class RequestGroupAnswerModel : ApiPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
        public string InterpreterLocation { get; set; }
        public InterpreterGroupAnswerModel InterpreterAnswer { get; set; }
        public InterpreterGroupAnswerModel ExtraInterpreterAnswer { get; set; }
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }
        public string BrokerReferenceNumber { get; set; }
    }
}
