using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class RequestGroupDetailsResponse : ResponseBase
    {
        public string Status { get; set; }
        // A LOT TO DO....
        // List orders here...
    }
}
