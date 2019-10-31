using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestGroupAnswerModel : ApiPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
        public InterpreterGroupAnswerModel InterpreterAnswer { get; set; }
        public InterpreterGroupAnswerModel ExtraInterpreterAnswer { get; set; }
    }
}
