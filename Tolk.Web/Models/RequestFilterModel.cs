using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequestFilterModel
    {
        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Myndighetens ärendenummer")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Län")]
        public int? RegionId { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganizationId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Datum för uppdrag")]
        public DateRange OrderDateRange { get; set; }

        [Display(Name = "Svarsdatum")]
        public DateRange AnswerByDateRange { get; set; }

        public RequestStatus? Status { get; set; }

        [Display(Name = "Besvarad av")]
        public int? AnsweredById { get; set; }

        internal IQueryable<RequestListRow> Apply(IQueryable<RequestListRow> items)
        {
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
            items = !string.IsNullOrWhiteSpace(OrderNumber)
              ? items.Where(i => i.EntityNumber.Contains(OrderNumber))
              : items;
            items = !string.IsNullOrWhiteSpace(CustomerReferenceNumber)
                ? items.Where(i => i.CustomerReferenceNumber != null && i.CustomerReferenceNumber.Contains(CustomerReferenceNumber))
                : items;
#pragma warning restore CA1307 // 
            items = RegionId.HasValue
                ? items.Where(i => i.RegionId == RegionId)
                : items;
            items = CustomerOrganizationId.HasValue
                ? items.Where(i => i.CustomerOrganisationId == CustomerOrganizationId)
                : items;
            items = LanguageId.HasValue
                ? items.Where(i => LanguageId == i.LanguageId)
                : items;
            items = OrderDateRange?.Start != null
                ? items.Where(i => i.StartAt.Date >= OrderDateRange.Start)
                : items;
            items = OrderDateRange?.End != null
                ? items.Where(i => i.StartAt.Date <= OrderDateRange.End)
                : items;
            items = AnswerByDateRange?.Start != null
                ? items.Where(i => i.ExpiresAt.Value.Date >= AnswerByDateRange.Start)
                : items;
            items = AnswerByDateRange?.End != null
                ? items.Where(i => i.ExpiresAt.Value.Date <= AnswerByDateRange.End)
                : items;
            items = Status.HasValue
                ? Status.Value == RequestStatus.ToBeProcessedByBroker
                    ? items.Where(r => r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)
                    : items.Where(r => r.Status == Status)
                : items;
            items = AnsweredById.HasValue
                ? items.Where(i => i.AnsweredBy == AnsweredById)
                : items;

            return items;
        }
    }
}
