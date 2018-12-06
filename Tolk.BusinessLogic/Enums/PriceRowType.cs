using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum PriceRowType
    {

        [Description("Tolkersättning")]
        InterpreterCompensation = 1,

        [Description("Sociala avgifter")]
        SocialInsuranceCharge = 2,

        [Description("Förmedlingsavgift")]
        BrokerFee = 3,

        [Description("Administrativ avgift")]
        AdministrativeCharge = 4,

        [Description("Resekostnad")]
        TravelCost = 5,

        [Description("Öresavrundning")]
        RoundedPrice = 6
    }
}
