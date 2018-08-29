﻿using System;
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

            // Compare start filter with end date date/time and end filter with
            // start date time to include occassions spanning midnight on filter date.
            requests = DateRange.Start.HasValue ?
                requests.Where(r => DateRange.Start <= r.Order.EndAt.Date)
                : requests;
            requests = DateRange.End.HasValue ?
                requests.Where(r => DateRange.End >= r.Order.StartAt.Date)
                : requests;

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
