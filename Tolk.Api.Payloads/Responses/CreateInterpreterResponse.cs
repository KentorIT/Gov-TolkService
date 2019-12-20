using Tolk.Api.Payloads.ApiPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class CreateInterpreterResponse : ResponseBase
    {
        public InterpreterDetailsModel Interpreter { get; set; }
    }
}
