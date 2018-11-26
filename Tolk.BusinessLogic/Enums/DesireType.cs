using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum DesireType
    {
        [Description("Krav på tolkens kompetensnivå finns")]
        Requirement = 1,

        [Description("Önskemål på tolkens kompetensnivå finns")]
        Request = 2
    }
}
