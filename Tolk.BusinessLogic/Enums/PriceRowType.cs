using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum PriceRowType
    {
        [CustomName("interpreter_compensation")]
        [Description("Tolkersättning")]
        [Parent(InvoiceableArticle.InterpreterCompensationIncludingSocialCharge)]
        InterpreterCompensation = 1,

        [Parent(InterpreterCompensation)]
        [CustomName("social_insurance_charge")]
        [Description("Sociala avgifter")]
        [Parent(InvoiceableArticle.InterpreterCompensationIncludingSocialCharge)]
        SocialInsuranceCharge = 2,

        [CustomName("broker_fee")]
        [Description("Förmedlingsavgift")]
        [Parent(InvoiceableArticle.BrokerFee)]
        BrokerFee = 3,

        [Parent(InterpreterCompensation)]
        [CustomName("administrative_charge")]
        [Description("Administrativ avgift")]
        [Parent(InvoiceableArticle.AdministrativeCharge)]
        AdministrativeCharge = 4,

        [CustomName("travel_cost")]
        [Description("Resekostnad")]
        [Parent(InvoiceableArticle.TravelCost)]
        TravelCost = 5,

        [CustomName("rounding")]
        [Description("Öresavrundning")]
        [Parent(InvoiceableArticle.InterpreterCompensationIncludingSocialCharge)]
        RoundedPrice = 6,

        [CustomName("outlay")]
        [Description("Utlägg för resa")]
        [Parent(InvoiceableArticle.TravelCost)]
        Outlay = 7,
    }
}
