﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

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

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }

        public bool HasActiveFilters => CreatedById.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || LanguageId.HasValue || DateRange?.Start != null || DateRange?.End != null || Status.HasValue || CustomerOrganisationId.HasValue || BrokerId.HasValue;

        public bool IsBroker { get; set; }

        [Display(Name = "Skapad av")]
        public int? CreatedById { get; set; }

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

            return requisitions;
        }
    }
}
