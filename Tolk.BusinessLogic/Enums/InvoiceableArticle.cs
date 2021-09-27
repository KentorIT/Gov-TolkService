using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum InvoiceableArticle
    {
        [Vat(0.25)]
        [CustomName("interpreter_compensation_incl_social_insurance")]
        [Description("Summa tolkersättning inkl.sociala avgifter")]
        InterpreterCompensationIncludingSocialCharge = 1,

        [Vat(0.25)]
        [CustomName("administrative_charge")]
        [Description("Administrativ avgift")]
        AdministrativeCharge = 2,

        [Vat(0.25)]
        [CustomName("broker_fee")]
        [Description("Förmedlingsavgift")]
        BrokerFee = 3,

        [Vat(0.25)]
        [CustomName("travel_cost")]
        [Description("Resekostnad")]
        TravelCost = 4
    }
}
