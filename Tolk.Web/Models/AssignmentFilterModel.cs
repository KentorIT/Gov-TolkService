using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Models
{
    public class AssignmentFilterModel
    {
        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Län")]
        public int? RegionId { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganizationId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Datum för uppdrag")]
        public DateRange DateRange { get; set; }

        [Display(Name = "Uppdragets status")]
        public AssignmentStatus? Status { get; set; }

        internal IQueryable<Request> Apply(IQueryable<Request> requests, ISwedishClock clock)
        {
            requests = !string.IsNullOrWhiteSpace(OrderNumber)
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
                ? requests.Where(r => r.Order.OrderNumber.Contains(OrderNumber))
#pragma warning restore CA1307 // 
                : requests;
            requests = RegionId.HasValue
                ? requests.Where(r => r.Order.RegionId == RegionId)
                : requests;
            requests = CustomerOrganizationId.HasValue
                ? requests.Where(r => r.Order.CustomerOrganisationId == CustomerOrganizationId)
                : requests;
            requests = LanguageId.HasValue
                ? requests.Where(r => r.Order.LanguageId == LanguageId)
                : requests;

            requests = DateRange?.Start != null
                ? requests.Where(r => r.Order.StartAt.Date >= DateRange.Start)
                : requests;
            requests = DateRange?.End != null
                ? requests.Where(r => r.Order.StartAt.Date <= DateRange.End)
                : requests;

            if (Status.HasValue)
            {
                switch (Status)
                {
                    case AssignmentStatus.ToBeExecuted:
                        requests = requests.Where(r => !r.Requisitions.Any() && r.Order.StartAt > clock.SwedenNow && r.Status == RequestStatus.Approved);
                        break;
                    case AssignmentStatus.ToBeReported:
                        requests = requests.Where(r => !r.Requisitions.Any() && r.Order.StartAt < clock.SwedenNow && r.Status == RequestStatus.Approved);
                        break;
                    case AssignmentStatus.Executed:
                        requests = requests.Where(r => r.Order.Status == OrderStatus.Delivered);
                        break;
                    case AssignmentStatus.Cancelled:
                        requests = requests.Where(r => r.Order.Status == OrderStatus.CancelledByBroker || r.Order.Status == OrderStatus.CancelledByCreator);
                        break;
                }
            }

            return requests;
        }
    }
}
