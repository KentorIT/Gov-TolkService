using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum TaxCardType
    {
        [CustomName("tax_card_a")]
        [Description("A-skatt")]
        TaxCardA = 1,

        [CustomName("tax_card_f")]
        [Description("F-skatt")]
        TaxCardF = 2,
    }
}
