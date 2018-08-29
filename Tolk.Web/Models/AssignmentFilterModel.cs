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

        [Display(Name = "Kund")]
        public int? CustomerOrganizationId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Datum")]
        public DateRange DateRange { get; set; }

        [Display(Name = "Uppdragets status")]
        public AssignmentStatus? Status { get; set; }

        internal IQueryable<Request> Apply(IQueryable<Request> requests, ISwedishClock clock)
        {
            // OrderNumber
            requests = !string.IsNullOrWhiteSpace(OrderNumber)
                ? requests.Where(r => r.Order.OrderNumber.Contains(OrderNumber))
                : requests;
            // Region
            requests = RegionId.HasValue
                ? requests.Where(r => r.Order.RegionId == RegionId)
                : requests;
            // CustomerOrganization
            requests = CustomerOrganizationId.HasValue
                ? requests.Where(r => r.Order.CustomerOrganisationId == CustomerOrganizationId)
                : requests;
            // Language
            requests = LanguageId.HasValue
                ? requests.Where(r => r.Order.LanguageId == LanguageId)
                : requests;
            // DateRange.Start
            requests = DateRange.Start.HasValue ?
                requests.Where(r => DateRange.Start <= r.Order.StartAt.Date)
                : requests;
            // DateRange.End
            requests = DateRange.End.HasValue ?
                requests.Where(r => DateRange.End >= r.Order.EndAt.Date)
                : requests;
            // Status
            if (Status.HasValue)
            {
                switch (Status)
                {
                    case AssignmentStatus.ToBeExecuted:
                        requests = requests.Where(r => !r.Requisitions.Any() && r.Order.StartAt > clock.SwedenNow);
                        break;
                    case AssignmentStatus.ToBeReported:
                        requests = requests.Where(r => !r.Requisitions.Any() && r.Order.StartAt < clock.SwedenNow);
                        break;
                    default:
                        requests = requests.Where(r => r.Requisitions.Any() && r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted);
                        break;
                }
            }

            return requests;
        }
    }
}
