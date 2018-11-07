using System;


namespace Tolk.Api.Payloads
{

    public class RequestCreatedModel : PayloadModel
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string OrderNumber { get; set; }
        public string Customer { get; set; }
        public string Region { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string Language { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }

    }
}
