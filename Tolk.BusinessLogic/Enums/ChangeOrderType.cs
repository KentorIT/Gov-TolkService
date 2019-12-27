using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum ChangeOrderType
    {

        [Description("På plats - gatuadress")]
        OnSiteStreet = 1,
        [Description("Anvisad lokal - gatuadress")]
        OffSiteDesignatedLocationStreet = 2,
        [Description("Kontaktuppgifter vid distanstolkning - telefon")]
        OffSitePhoneContactInformation = 3,
        [Description("Kontaktuppgifter vid distanstolkning - video")]
        OffSiteVideoContactInformation = 4,
        [Description("Övrig information om uppdraget")]
        Description = 5,
        [Description("Fakturareferens")]
        InvoiceReference = 6,
        [Description("Myndighetens ärendenummer")]
        CustomerReferenceNumber = 7,
        [Description("Myndighetens avdelning")]
        CustomerDepartment = 8,

    }
}
