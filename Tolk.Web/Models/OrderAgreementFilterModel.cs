using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class OrderAgreementFilterModel
    {
        [Display(Name = "BokningsID")]
        [Placeholder("Ange del av BokningsID")]
        public string SearchOrderNumber { get; set; }
        [Display(Name = "Skapat datum")]
        public DateRange DateCreated { get; set; }

        [NoDisplayName]
        public string FilterMessage { get; set; }

        public bool IsAdmin { get; set; }

        public int? CustomerOrganisationId { get; set; }

        internal IQueryable<OrderAgreementPayload> Apply(IQueryable<OrderAgreementPayload> payloads)
        {
            payloads = CustomerOrganisationId.HasValue ? payloads.Where(p => p.Request.Order.CustomerOrganisationId == CustomerOrganisationId) : payloads;
            payloads = DateCreated?.Start != null ? payloads.Where(p => p.CreatedAt.Date >= DateCreated.Start) : payloads;
            payloads = DateCreated?.End != null ? payloads.Where(p => p.CreatedAt.Date <= DateCreated.End) : payloads;
            payloads = !string.IsNullOrWhiteSpace(SearchOrderNumber) ? payloads.Where(p => p.Request.Order.OrderNumber.Contains(SearchOrderNumber)) : payloads;
            return payloads;
        }
    }
}
