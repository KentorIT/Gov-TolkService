using System;

namespace Tolk.Api.Payloads.Responses
{
    public class ResponseBase
    {
        public virtual bool Success { get; set; } = true;
        public int StatusCode { get; set; } = 200;
    }
}
