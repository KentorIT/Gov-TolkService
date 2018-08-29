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
    public class RequisitionFilterModel
    {
        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Datum")]
        public DateRange DateRange { get; set; }

        public RequisitionStatus? Status { get; set; }
        [Display(Name = "Filtrera på kontaktperson")]
        public bool? FilterByContact { get; set; }
        public bool IsCustomer { get; set; }

        internal IQueryable<Requisition> Apply(IQueryable<Requisition> requisitions)
        {
            requisitions = !string.IsNullOrWhiteSpace(OrderNumber)
                ? requisitions.Where(r => r.Request.Order.OrderNumber.Contains(OrderNumber))
                : requisitions;
            requisitions = LanguageId.HasValue
                ? requisitions.Where(r => r.Request.Order.LanguageId == LanguageId)
                : requisitions;
            requisitions = DateRange?.Start != null
                ? requisitions = requisitions.Where(r => DateRange.Start <= r.Request.Order.EndAt.Date)
                : requisitions;
            requisitions = DateRange?.End != null
                ? requisitions = requisitions.Where(r => DateRange.End >= r.Request.Order.StartAt.Date)
                : requisitions;
            requisitions = Status.HasValue
                ? requisitions = requisitions.Where(r => r.Status == Status)
                : requisitions;

            return requisitions;
        }
    }
}
