using System;
using System.Collections.Generic;
using System.Text;
using Tolk.Api.Payloads.ApiPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class BrokerInterpretersResponse : ResponseBase
    {
        public IEnumerable<InterpreterModel> Interpreters { get; set; }
    }
}
