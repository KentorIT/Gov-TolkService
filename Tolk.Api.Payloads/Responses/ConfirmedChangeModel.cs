using System;

namespace Tolk.Api.Payloads.Responses
{
    public class ConfirmedChangeModel
    {
        public DateTimeOffset ChangedAt { get; set; }

        public string ChangeType { get; set; }
 
    }
}
