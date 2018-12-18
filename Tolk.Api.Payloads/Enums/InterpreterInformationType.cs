using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Api.Payloads.Enums
{
    public enum InterpreterInformationType
    {
        [CustomName("new_interpreter")]
        [Description("Ny tolk")]
        NewInterpreter = 1,
        [CustomName("existing_interpreter")]
        [Description("Redan registrerad tolk")]
        ExistingInterpreter = 2,
        [CustomName("authorized_interpreter_id")]
        [Description("Tolk-ID")]
        AuthorizedInterpreterId = 3
    }
}
