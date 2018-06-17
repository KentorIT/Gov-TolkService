using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequisitionFilterModel
    {
        public RequisitionStatus? Status { get; set; }
        [Display(Name = "Filtrera på kontaktperson")]
        public bool? FilterByContact { get; set; }
        public bool IsCustomer { get; set; }
    }
}
