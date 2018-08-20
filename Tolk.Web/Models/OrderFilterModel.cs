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
        [Display(Name = "Starttidsintervall")]
        public DateRange StartTimeRange { get; set; }

        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        public OrderStatus? Status { get; set; }

        [Display(Name = "Region")]
        public int? RegionId { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }
    }
}
