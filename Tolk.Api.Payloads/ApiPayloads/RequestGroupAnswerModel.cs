namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestGroupAnswerModel : ApiPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
        public string InterpreterLocation { get; set; }
        //Files
        public InterpreterGroupAnswerModel InterpreterAnswer { get; set; }
        public InterpreterGroupAnswerModel ExtraInterpreterAnswer { get; set; }
    }
}
