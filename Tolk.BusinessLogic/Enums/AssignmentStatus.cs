using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;


namespace Tolk.BusinessLogic.Enums
{
    public enum AssignmentStatus
    {
        [Description("Kommande uppdrag")]
        ToBeExecuted = 1,
        [Description("Uppdrag att avrapportera")]
        ToBeReported = 2,
        [Description("Utförda uppdrag")]
        Executed = 3,
    }
}
