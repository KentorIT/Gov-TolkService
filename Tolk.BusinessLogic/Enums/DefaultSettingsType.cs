using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Enums
{
    public enum DefaultSettingsType
    {
        [Description("Region/Län")]
        Region = 1,
        [Description("Enhet")]
        CustomerUnit = 2,
        [Description("Inställelsesätt - första hand")]
        InterpreterLocationPrimary = 3,
        [Description("Inställelsesätt - andra hand")]
        InterpreterLocationSecondary = 4,
        [Description("Inställelsesätt - tredje hand")]
        InterpreterLocationThird = 5,
        [Description("På plats - gatuadress")]
        OnSiteStreet = 6,
        [Description("På plats - ort")]
        OnSiteCity = 7,
        [Description("Anvisad lokal - gatuadress")]
        OffSiteDesignatedLocationStreet = 8,
        [Description("Anvisad lokal - ort")]
        OffSiteDesignatedLocationCity = 9,

        [Description("Kontaktuppgifter vid distanstolkning - telefon")]
        OffSitePhoneContactInformation = 10,
        [Description("Kontaktuppgifter vid distanstolkning - video")]
        OffSiteVideoContactInformation = 11,

        [Description("Accepterar restid eller resvä som överskrider gränsvärden")]
        AllowExceedingTravelCost = 12,

        [Description("Fakturareferens")]
        InvoiceReference = 13,
    }
}
