using System;

namespace Tolk.Api.Payloads.Responses
{
    public class ConfirmedUpdateModel
    {
        public DateTimeOffset UpdatedAt { get; set; }

        public string RequestUpdateType { get; set; }

    }
}
