using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class CustomerInformationModel
    {
        [Display(Name = "Myndighetens enhet")]
        public string UnitName { get; set; }

        [Display(Name = "Myndighetens avdelning")]
        public string DepartmentName { get; set; }

        [Display(Name = "Fakturareferens")]
        public string InvoiceReference { get; set; }

        [Display(Name = "Myndighet")]
        public string Name { get; set; }

        [Display(Name = "Myndighetens organisationsnummer")]
        public string OrganisationNumber { get; set; }

        [Display(Name = "Myndighetens ärendenummer")]
        public string ReferenceNumber { get; set; }

        [Display(Name = "Tolken fakturerar själv tolkarvode")]
        public bool UseSelfInvoicingInterpreter { get; set; }

        [Display(Name = "Bokning skapad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }

        public bool IsCustomer { get; set; }

    }
}
