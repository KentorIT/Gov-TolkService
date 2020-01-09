using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum ChangeOrderType
    {
        [Description("Gatuadress för lokal")]
        LocationStreet = 1,
        [Description("Kontaktuppgifter vid distanstolkning")]
        OffSiteContactInformation = 2,
        [Description("Övrig information om uppdraget")]
        Description = 3,
        [Description("Fakturareferens")]
        InvoiceReference = 4,
        [Description("Myndighetens ärendenummer")]
        CustomerReferenceNumber = 5,
        [Description("Myndighetens avdelning")]
        CustomerDepartment = 6
    }
}
