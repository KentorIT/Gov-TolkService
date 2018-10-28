using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Models
{
    public class AssignmentFilterModel
    {
        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Region")]
        public int? RegionId { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganizationId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Startdatum")]
        public DateRange DateRange { get; set; }

        [Display(Name = "Uppdragets status")]
        public AssignmentStatus? Status { get; set; }

        public bool HasActiveFilters
        {
            get => RegionId.HasValue|| CustomerOrganizationId.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || LanguageId.HasValue || DateRange?.Start != null || DateRange?.End != null || Status.HasValue; 
        }

        internal IQueryable<Request> Apply(IQueryable<Request> requests, ISwedishClock clock)
        {
            requests = !string.IsNullOrWhiteSpace(OrderNumber)
                ? requests.Where(r => r.Order.OrderNumber.Contains(OrderNumber))
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
                        requests = requests.Where(r => r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted);
                        break;
                    case AssignmentStatus.Cancelled:
                        requests = requests.Where(r => r.Order.Status == OrderStatus.CancelledByBroker || r.Order.Status == OrderStatus.CancelledByBrokerConfirmed || r.Order.Status == OrderStatus.CancelledByCreator || r.Order.Status == OrderStatus.CancelledByCreatorConfirmed);
                        break;
                }
            }

            return requests;
        }
    }
}
