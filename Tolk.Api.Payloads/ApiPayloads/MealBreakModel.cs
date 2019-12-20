using System;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class MealBreakModel
    {
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset EndedAt { get; set; }
    }
}
