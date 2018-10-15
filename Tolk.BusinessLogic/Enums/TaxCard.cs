using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum TaxCard
    {
        [Description("A-skatt")]
        TaxCardA = 1,

        [Description("F-skatt")]
        TaxCardF = 2,
    }
}
