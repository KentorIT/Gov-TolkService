using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum DesireType
    {
        [Description("Önskemål om kompetensnivå")]
        Request = 1,

        [Description("Krav på kompetensnivå")]
        Requirement = 2,
    }
}
