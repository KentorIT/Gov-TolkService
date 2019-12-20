using System;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class ErrorMessageModel : WebHookPayloadBaseModel
    {
        public DateTimeOffset ReportedAt { get; set; }
        public int CallId { get; set; }
        public string NotificationType { get; set; }
    }
}


