using System;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class FlexibleRequestModel : RequestModel
    {
        public FlexibleRequestModel() { }

        public DateTimeOffset FlexibleStartAt { get; set; }
        public DateTimeOffset FlexibleEndAt { get; set; }
        public TimeSpan? ExpectedLength { get; set; }
    }
}
