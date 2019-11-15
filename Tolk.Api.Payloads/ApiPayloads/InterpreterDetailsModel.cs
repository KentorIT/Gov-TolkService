namespace Tolk.Api.Payloads.ApiPayloads
{
    public class InterpreterDetailsModel : InterpreterModel
    {
        public InterpreterDetailsModel() { }

        public InterpreterDetailsModel(InterpreterModel model)
        {
            FirstName = model.FirstName;
            LastName = model.LastName;
            Email = model.Email;
            PhoneNumber = model.PhoneNumber;
            OfficialInterpreterId = model.OfficialInterpreterId;
            InterpreterId = model.InterpreterId;
            InterpreterInformationType = model.InterpreterInformationType;
        }
        public bool IsActive { get; set; } = true;
    }
}
