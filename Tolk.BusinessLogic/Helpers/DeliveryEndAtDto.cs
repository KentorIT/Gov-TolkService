using System;
namespace Tolk.BusinessLogic.Helpers
{
    public class DeliveryEndAtDto
    {
        public TimeSpan? ExpectedLength { get; set; }
        public DateTimeOffset? RespondedStartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public DateTimeOffset CalculatedEndAt => RespondedStartAt.HasValue ? RespondedStartAt.Value.Add(ExpectedLength.Value) : EndAt;
    }

}