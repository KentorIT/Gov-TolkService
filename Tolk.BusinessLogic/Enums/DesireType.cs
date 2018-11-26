using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum DesireType
    {
        [Description("Önskemål på tolkens kompetensnivå finns")]
        Request = 1,

        [Description("Krav på tolkens kompetensnivå finns")]
        Requirement = 2,
    }
}
