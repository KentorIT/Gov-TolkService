using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class InterpreterLocationAddressModel
    {
        [Display(Name = "Adress")]
        [ClientRequired]
        public string LocationStreet { get; set; }

        [Display(Name = "Postnummer")]
        [ClientRequired]
        [RegularExpression("[0-9]{3} ?[0-9]{2}", ErrorMessage = "Ange postnummer enligt format 12345 eller 123 45")]
        public string LocationZipCode { get; set; }

        [Display(Name = "Ort")]
        [ClientRequired]
        public string LocationCity { get; set; }

        [Display(Name = "Typ av distanstolkning")]
        [ClientRequired]
        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        [ClientRequired]
        [StringLength(255)]
        public string OffSiteContactInformation { get; set; }
    }
}
