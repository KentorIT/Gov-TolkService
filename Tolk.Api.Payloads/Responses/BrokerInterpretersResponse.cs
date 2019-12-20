using System.Collections.Generic;
using Tolk.Api.Payloads.ApiPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class BrokerInterpretersResponse : ResponseBase
    {
        public IEnumerable<InterpreterDetailsModel> Interpreters { get; set; }
    }
}
