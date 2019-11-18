using System;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class InterpreterDetailsModel : InterpreterModel
    {
        public InterpreterDetailsModel() { }

        public InterpreterDetailsModel(InterpreterModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException($"Argument is null in class {nameof(InterpreterDetailsModel)}, method {nameof(InterpreterDetailsModel)}");
            }

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
