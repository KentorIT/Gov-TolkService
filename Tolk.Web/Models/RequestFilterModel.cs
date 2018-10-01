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

namespace Tolk.Web.Models
{
    public class RequestFilterModel
    {
        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Region")]
        public int? RegionId { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganizationId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Startdatum för tolkning")]
        public DateRange OrderDateRange { get; set; }

        [Display(Name = "Svarsdatum")]
        public DateRange AnswerByDateRange { get; set; }

        public RequestStatus? Status { get; set; }

        internal IQueryable<RequestListItemModel> Apply(IQueryable<RequestListItemModel> items)
        {
            items = !string.IsNullOrWhiteSpace(OrderNumber)
                ? items.Where(i => i.OrderNumber.Contains(OrderNumber))
                : items;
            items = RegionId.HasValue
                ? items.Where(i => i.RegionName == Region.Regions.Single(r => r.RegionId == RegionId).Name)
                : items;
            items = CustomerOrganizationId.HasValue
                ? items.Where(i => CustomerOrganizationId == i.CustomerId)
                : items;
            items = LanguageId.HasValue
                ? items.Where(i => LanguageId == i.LanguageId)
                : items;
            items = OrderDateRange?.Start != null
                ? items.Where(i => i.Start.Date >= OrderDateRange.Start)
                : items;
            items = OrderDateRange?.End != null
                ? items.Where(i => i.Start.Date <= OrderDateRange.End)
                : items;
            items = AnswerByDateRange?.Start != null
                ? items.Where(i => i.ExpiresAt.Value.Date >= AnswerByDateRange.Start)
                : items;
            items = AnswerByDateRange?.End != null
                ? items.Where(i => i.ExpiresAt.Value.Date <= AnswerByDateRange.End)
                : items;
            items = Status.HasValue
                ? Status.Value == RequestStatus.ToBeProcessedByBroker ? items.Where(r => r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) : items.Where(r => r.Status == Status)
                 : items;

            return items;
        }
    }
}
