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
    public class OrderFilterModel
    {
        [Display(Name = "Start")]
        public DateTimeOffset? StartAt { get; set; }

        [Display(Name = "Slut")]
        public DateTimeOffset? EndAt { get; set; }

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
