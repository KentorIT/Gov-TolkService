using System.ComponentModel.DataAnnotations;
using System.Text;
using E = Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class InterpreterLocationAddressModel
    {
        public E.InterpreterLocation? InterpreterLocation { get; set; }

        public int? Rank { get; set; }

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
        public E.OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        [ClientRequired]
        [StringLength(255)]
        public string OffSiteContactInformation { get; set; }

        [DataType(DataType.MultilineText)]
        [NoDisplayName]
        public string CompactInformation
        {
            get
            {
                if (!InterpreterLocation.HasValue)
                {
                    return string.Empty;
                }
                string rankHeader = string.Empty;
                switch (Rank)
                    {
                    case 2:
                        rankHeader = "I andra hand:\n";
                        break;
                    case 3:
                        rankHeader = "I tredje hand:\n";
                        break;
                    default:
                        break;
                }
                StringBuilder sb = new StringBuilder($"{rankHeader}{InterpreterLocation.Value.GetDescription()}");
                if (InterpreterLocation.Value == E.InterpreterLocation.OffSite)
                {
                    sb.Append($"\n{OffSiteAssignmentType.Value.GetDescription()}: {OffSiteContactInformation}");
                }
                else
                {
                    sb.Append($"\n{LocationStreet}\n{LocationZipCode} {LocationCity}");
                }
                return sb.ToString();
            }
        }
    }
}
