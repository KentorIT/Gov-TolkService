using System.ComponentModel.DataAnnotations;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;
using E = Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class InterpreterLocationAddressModel : IModel
    {

        [Display(Name = "Inställelsesätt")]
        public E.InterpreterLocation? InterpreterLocation { get; set; }

        public int? Rank { get; set; }

        [Display(Name = "Gatuadress", Description = "Ange tydlig gatuadress så att tolken hittar. Mer information kan anges i fältet för övrig information om uppdraget.")]
        [ClientRequired]
        [SubItem]
        [StringLength(100)]
        public string LocationStreet { get; set; }

        [Display(Name = "Ort")]
        [ClientRequired]
        [SubItem]
        [StringLength(100)]
        public string LocationCity { get; set; }

        [Display(Name = "Kontaktinformation för tolktillfället", Description = "Distans per video: Ange vilket system som ska användas för videomötet. Mer information om anslutning etc. kan anges i fältet för övrig information om uppdraget. Distans per telefon: Ange vilket telefonnummer tolken ska ringa upp på eller om ni istället själva vill ringa upp tolken.")]
        [ClientRequired]
        [StringLength(255)]
        [SubItem]
        public string OffSiteContactInformation { get; set; }

        [DataType(DataType.MultilineText)]
        [NoDisplayName]
        public string CompactInformationWithRankHeader
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
                return $"{rankHeader}{CompactInformation}";
            }
        }

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
                StringBuilder sb = new StringBuilder(InterpreterLocation.Value.GetDescription());
                return sb.Append(IsOffsite ? $"\nKontaktinformation:\n{OffSiteContactInformation}" : $"\nAdress:\n{LocationStreet}, {LocationCity}").ToString();
            }
        }

        [Display(Name = "Adress")]
        [SubItem]
        public string Address => InterpreterLocation.HasValue ? !IsOffsite ? $"{LocationStreet}, {LocationCity}" : string.Empty : string.Empty;

        private bool IsOffsite => InterpreterLocation.HasValue && (InterpreterLocation.Value == E.InterpreterLocation.OffSitePhone || InterpreterLocation.Value == E.InterpreterLocation.OffSiteVideo);

        internal  OrderInterpreterLocation GetInterpreterLocation(E.InterpreterLocation location, int rank)
        {
            return new OrderInterpreterLocation
            {
                InterpreterLocation = location,
                Rank = rank,
                Street = (location == E.InterpreterLocation.OffSiteDesignatedLocation || location == E.InterpreterLocation.OnSite) ? LocationStreet : null,
                City = (location == E.InterpreterLocation.OffSiteDesignatedLocation || location == E.InterpreterLocation.OnSite) ? LocationCity : null,
                OffSiteContactInformation = (location == E.InterpreterLocation.OffSiteVideo || location == E.InterpreterLocation.OffSitePhone) ? OffSiteContactInformation : null,
            };
        }


    }
}
