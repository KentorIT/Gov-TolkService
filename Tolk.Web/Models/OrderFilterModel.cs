﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class OrderFilterModel
    {
        [Display(Name = "Startdatum för tolkning")]
        public DateRange DateRange { get; set; }

        [Display(Name = "Boknings-ID")]
        public string OrderNumber { get; set; }

        public OrderStatus? Status { get; set; }

        [Display(Name = "Län")]
        public int? RegionId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }

        [Display(Name = "Skapad av")]
        public int? CreatedBy { get; set; }

        public bool IsSuperUser { get; set; }

        public bool HasActiveFilters
        {
            get => RegionId.HasValue || CreatedBy.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || LanguageId.HasValue || DateRange?.Start != null || DateRange?.End != null || Status.HasValue || BrokerId.HasValue; 
        }

        internal IQueryable<Order> Apply(IQueryable<Order> orders)
        {
            orders = !string.IsNullOrWhiteSpace(OrderNumber) 
                ? orders.Where(o => o.OrderNumber.Contains(OrderNumber)) 
                : orders;
            orders = RegionId.HasValue 
                ? orders.Where(o => o.RegionId == RegionId) 
                : orders;
            orders = LanguageId.HasValue 
                ? orders.Where(o => o.LanguageId == LanguageId) 
                : orders;
            orders = CreatedBy.HasValue
                ? orders.Where(o => o.CreatedBy == CreatedBy)
                : orders;
            orders = Status.HasValue 
                ? Status.Value == OrderStatus.ToBeProcessedByCustomer 
                    ? orders.Where(o => o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.RequestRespondedNewInterpreter) 
                : orders.Where(o => o.Status == Status) : orders;
            orders = BrokerId.HasValue 
                ? orders.Where(o => o.Requests.Any(req => req.Ranking.BrokerId == BrokerId && (
                        req.Status == RequestStatus.Created ||
                        req.Status == RequestStatus.Received ||
                        req.Status == RequestStatus.Accepted ||
                        req.Status == RequestStatus.Approved ||
                        req.Status == RequestStatus.AcceptedNewInterpreterAppointed))) 
                : orders;

            orders = DateRange?.Start != null
                    ? orders.Where(o => o.StartAt.Date >= DateRange.Start)
                    : orders;
            orders = DateRange?.End != null
                    ? orders.Where(o => o.StartAt.Date <= DateRange.End)
                    : orders;
                
            return orders;
        }
    }
}
