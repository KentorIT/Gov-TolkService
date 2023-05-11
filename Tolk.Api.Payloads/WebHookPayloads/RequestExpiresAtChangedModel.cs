using System;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestExpiresAtChangedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}