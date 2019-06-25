using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestGroupLostDueToInactivityModel : WebHookPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
    }
}
