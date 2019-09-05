﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class OrderFilterModel
    {
        [Display(Name = "Datum för uppdrag")]
        public DateRange DateRange { get; set; }

        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Myndighetens ärendenummer")]
        public string CustomerReferenceNumber { get; set; }

        public OrderStatus? Status { get; set; }

        [Display(Name = "Län")]
        public int? RegionId { get; set; }

        [Display(Name = "Myndighetens enhet")]
        public int? CustomerUnitId { get; set; }

        [Display(Name = "Visa inte bokningar för inaktiva enheter")]
        public bool? FilterByInactiveUnits { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganisationId { get; set; }

        [Display(Name = "Skapad av")]
        public int? CreatedBy { get; set; }

        public bool IsCentralAdminOrOrderHandler { get; set; }

        public bool HasCustomerUnits => CustomerUnits != null && CustomerUnits.Any();
        public IEnumerable<int> CustomerUnits { get; set; }

        public bool IsAdmin { get; set; }

        public bool HasActiveFilters
        {
            get => RegionId.HasValue || CreatedBy.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || !string.IsNullOrWhiteSpace(CustomerReferenceNumber) || 
                LanguageId.HasValue || DateRange?.Start != null || DateRange?.End != null || Status.HasValue || BrokerId.HasValue || CustomerOrganisationId.HasValue; 
        }

        public int UserId { get; set; }

        internal IQueryable<Order> Apply(IQueryable<Order> orders)
        {
            if (!IsAdmin)
            {
                orders = orders.CustomerOrders(IsCentralAdminOrOrderHandler, CustomerOrganisationId.Value, UserId, CustomerUnits);
            }

            orders = !string.IsNullOrWhiteSpace(OrderNumber) 
                ? orders.Where(o => o.OrderNumber.Contains(OrderNumber)) 
                : orders;
            orders = !string.IsNullOrWhiteSpace(CustomerReferenceNumber)
                ? orders.Where(o => o.CustomerReferenceNumber.Contains(CustomerReferenceNumber))
                : orders;
            orders = RegionId.HasValue 
                ? orders.Where(o => o.RegionId == RegionId) 
                : orders;
            orders = CustomerUnitId.HasValue
                ? orders.Where(o => o.CustomerUnitId == CustomerUnitId)
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
                ? orders.Where(o => o.Requests.Any(req => req.Ranking.BrokerId == BrokerId && (req.IsToBeProcessedByBroker || req.IsAcceptedOrApproved))) 
                : orders;
            orders = CustomerOrganisationId.HasValue
                ? orders.Where(o => o.CustomerOrganisationId == CustomerOrganisationId)
                : orders;
            orders = FilterByInactiveUnits.HasValue
                ? orders.Where(o => o.CustomerUnit == null || o.CustomerUnit.IsActive)
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
