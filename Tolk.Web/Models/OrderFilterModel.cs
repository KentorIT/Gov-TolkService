using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderFilterModel
    {
        [Display(Name = "Datum")]
        public DateRange DateRange { get; set; }

        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        public OrderStatus? Status { get; set; }

        [Display(Name = "Region")]
        public int? RegionId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }

        internal IQueryable<Order> Apply(IQueryable<Order> orders)
        {
            orders = !string.IsNullOrWhiteSpace(OrderNumber) 
                ? orders.Where(o => o.OrderNumber.Contains(OrderNumber)) 
                : orders;
            orders = RegionId.HasValue 
                ? orders.Where(o => o.Region.RegionId == RegionId) 
                : orders;
            orders = LanguageId.HasValue 
                ? orders.Where(o => o.Language.LanguageId == LanguageId) 
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

            // Compare start filter with end date date/time and end filter with
            // start date time to include occassions spanning midnight on filter date.
            orders = DateRange?.Start != null
                    ? orders.Where(o => DateRange.Start <= o.EndAt.Date)
                    : orders;
            orders = DateRange?.End != null
                    ? orders.Where(o => DateRange.End >= o.StartAt.Date)
                    : orders;
                
            return orders;
        }
    }
}
