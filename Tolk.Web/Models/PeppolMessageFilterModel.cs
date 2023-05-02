using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class PeppolMessageFilterModel
    {
        [Display(Name = "Identifierare")]
        [Placeholder("Ange del av Identifierare")]
        public string SearchIdentifier { get; set; }
        [Display(Name = "Skapat datum")]
        public DateRange DateCreated { get; set; }

        [NoDisplayName]
        public string FilterMessage { get; set; }

        public bool IsAdmin { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganisationId { get; set; }

        [Display(Name = "Är senaste")]
        public TrueFalse? IsLatest { get; set; }

        internal IQueryable<PeppolPayload> Apply(IQueryable<PeppolPayload> payloads)
        {
            payloads = CustomerOrganisationId.HasValue ? payloads.Where(p => p.Request.Order.CustomerOrganisationId == CustomerOrganisationId) : payloads;
            payloads = DateCreated?.Start != null ? payloads.Where(p => p.CreatedAt.Date >= DateCreated.Start) : payloads;
            payloads = DateCreated?.End != null ? payloads.Where(p => p.CreatedAt.Date <= DateCreated.End) : payloads;
            payloads = !string.IsNullOrWhiteSpace(SearchIdentifier) ? payloads.Where(p => p.IdentificationNumber.Contains(SearchIdentifier)) : payloads;
            payloads = IsLatest.HasValue ? payloads.Where(e => e.ReplacedById.HasValue == (IsLatest == TrueFalse.No)) : payloads;
            return payloads;
        }
    }
}
