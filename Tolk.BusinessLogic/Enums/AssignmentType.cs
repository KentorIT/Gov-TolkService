using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum AssignmentType
    {
        [Description("Tolkning")]
        Interpretation = 1,
        [Description("Tolkanvändarutbildning")]
        Education = 2,
        [Description("Avista")]
        Avista = 3,
    }
}
