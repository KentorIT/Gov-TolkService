using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class RequisitionFilterModel
    {
        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Datum för uppdrag")]
        public DateRange DateRange { get; set; }

        public RequisitionStatus? Status { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganisationId { get; set; }

        [Display(Name = "Myndighetens enhet")]
        public int? CustomerUnitId { get; set; }

        [Display(Name = "Visa inte rekvisitioner för inaktiva enheter")]
        public bool? FilterByInactiveUnits { get; set; }

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }

        public bool HasCustomerUnits => CustomerUnits != null && CustomerUnits.Any();

        public bool HasActiveFilters => CreatedById.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || LanguageId.HasValue || DateRange?.Start != null || DateRange?.End != null || Status.HasValue || CustomerOrganisationId.HasValue || BrokerId.HasValue;

        public bool IsBroker { get; set; }

        [Display(Name = "Skapad av")]
        public int? CreatedById { get; set; }

        public bool IsCentralAdminOrOrderHandler { get; set; }

        public bool IsAdmin { get; set; }

        public int UserId{ get; set; }


        public IEnumerable<int> CustomerUnits { get; set; }

        internal IQueryable<Requisition> GetRequisitionsFromOrders(IQueryable<Order> orders)
        {
            if (!IsAdmin)
            {
                orders = orders.CustomerOrders(CustomerOrganisationId.Value, UserId, CustomerUnits, IsCentralAdminOrOrderHandler, true);
            }
            return orders.Select(o => o.Requests).SelectMany(r => r).SelectMany(r => r.Requisitions)
                .Where(r => !r.ReplacedByRequisitionId.HasValue);
        }

        internal IQueryable<Requisition> GetRequisitionsFromRequests(IQueryable<Request> requests)
        {
            return requests.BrokerRequests(BrokerId.Value).SelectMany(r => r.Requisitions)
                .Where(r => !r.ReplacedByRequisitionId.HasValue);
        }

        internal IQueryable<Requisition> Apply(IQueryable<Requisition> requisitions)
        {
            requisitions = !string.IsNullOrWhiteSpace(OrderNumber)
                ? requisitions.Where(r => r.Request.Order.OrderNumber.Contains(OrderNumber))
                : requisitions;
            requisitions = LanguageId.HasValue
                ? requisitions.Where(r => r.Request.Order.LanguageId == LanguageId)
                : requisitions;
            requisitions = DateRange?.Start != null
                ? requisitions = requisitions.Where(r => r.Request.Order.StartAt.Date >= DateRange.Start)
                : requisitions;
            requisitions = DateRange?.End != null
                ? requisitions = requisitions.Where(r => r.Request.Order.StartAt.Date <= DateRange.End)
                : requisitions;
            requisitions = Status.HasValue
                ? requisitions = requisitions.Where(r => r.Status == Status)
                : requisitions;
            requisitions = CreatedById.HasValue
                ? requisitions = requisitions.Where(r => r.CreatedBy == CreatedById)
                : requisitions;
            requisitions = CustomerOrganisationId.HasValue
                ? requisitions = requisitions.Where(r => r.Request.Order.CustomerOrganisationId == CustomerOrganisationId)
                : requisitions;
            requisitions = BrokerId.HasValue
                  ? requisitions = requisitions.Where(r => r.Request.Ranking.BrokerId == BrokerId)
                  : requisitions;
            requisitions = CustomerUnitId.HasValue
                ? requisitions.Where(r => r.Request.Order.CustomerUnitId == CustomerUnitId)
                : requisitions;
            requisitions = FilterByInactiveUnits ?? false
                ? requisitions.Where(r => r.Request.Order.CustomerUnit == null || r.Request.Order.CustomerUnit.IsActive)
                : requisitions;

            return requisitions;

        }
    }
}
