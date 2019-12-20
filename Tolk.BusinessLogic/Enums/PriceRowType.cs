using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum PriceRowType
    {
        [CustomName("interpreter_compensation")]
        [Description("Tolkersättning")]
        InterpreterCompensation = 1,

        [Parent(InterpreterCompensation)]
        [CustomName("social_insurance_charge")]
        [Description("Sociala avgifter")]
        SocialInsuranceCharge = 2,

        [CustomName("broker_fee")]
        [Description("Förmedlingsavgift")]
        BrokerFee = 3,

        [Parent(InterpreterCompensation)]
        [CustomName("administrative_charge")]
        [Description("Administrativ avgift")]
        AdministrativeCharge = 4,

        [CustomName("travel_cost")]
        [Description("Resekostnad")]
        TravelCost = 5,

        [CustomName("rounding")]
        [Description("Öresavrundning")]
        RoundedPrice = 6,

        [CustomName("outlay")]
        [Description("Utlägg för resa")]
        Outlay = 7,

    }
}
