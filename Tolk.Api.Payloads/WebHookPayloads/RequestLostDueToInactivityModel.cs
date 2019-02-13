using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestLostDueToInactivityModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
    }
}
