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

        public bool HasActiveFilters
        {
            get => AnsweredById.HasValue || RegionId.HasValue || CustomerOrganizationId.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || !string.IsNullOrWhiteSpace(OrderNumber) || 
                LanguageId.HasValue || OrderDateRange?.Start != null || OrderDateRange?.End != null || AnswerByDateRange?.Start != null || AnswerByDateRange?.End != null || Status.HasValue; 
        }

        internal IQueryable<Request> Apply(IQueryable<Request> items)
        {
            items = items.Where(i => i.Status != RequestStatus.AwaitingDeadlineFromCustomer &&
                    i.Status != RequestStatus.NoDeadlineFromCustomer &&
                    i.Status != RequestStatus.InterpreterReplaced);
            items = !string.IsNullOrWhiteSpace(OrderNumber)
                ? items.Where(i => i.Order.OrderNumber.Contains(OrderNumber))
                : items;
            items = !string.IsNullOrWhiteSpace(CustomerReferenceNumber)
                ? items.Where(i => i.Order.CustomerReferenceNumber.Contains(CustomerReferenceNumber))
                : items;
            items = RegionId.HasValue
                ? items.Where(i => i.Order.RegionId == RegionId)
                : items;
            items = CustomerOrganizationId.HasValue
                ? items.Where(i => i.Order.CustomerOrganisationId == CustomerOrganizationId)
                : items;
            items = LanguageId.HasValue
                ? items.Where(i => LanguageId == i.Order.LanguageId)
                : items;
            items = OrderDateRange?.Start != null
                ? items.Where(i => i.Order.StartAt.Date >= OrderDateRange.Start)
                : items;
            items = OrderDateRange?.End != null
                ? items.Where(i => i.Order.StartAt.Date <= OrderDateRange.End)
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
