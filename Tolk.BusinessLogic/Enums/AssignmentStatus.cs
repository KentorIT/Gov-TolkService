using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;


namespace Tolk.BusinessLogic.Enums
{
    public enum AssignmentStatus
    {
        [Description("Tillsättning är godkänd")]
        ToBeExecuted = 1,
        [Description("Uppdrag väntar avrapportering")]
        ToBeReported = 2,
        [Description("Uppdrag är utfört")]
        Executed = 3,
        [Description("Uppdrag är avbokat")]
        Cancelled = 4
    }
}
