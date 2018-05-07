using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class OrderModel
    {
        [Display(Name = "Län")]
        [Required]
        public int County { get; set; }

        [Display(Name = "Språk")]
        [Required]
        public int Language { get; set; }

        [Display(Name = "Plats")]
        [Required]
        public string LocationName { get; set; }

        [Display(Name = "Adress")]
        [Required]
        public string LocationAddress { get; set; }

        [Display(Name = "Ort")]
        [Required]
        public string LocationCity { get; set; }

        [Display(Name = "Startdatum och tid")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "Slutdatum och tid")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Typ av tolkuppdrag")]
        [Required]
        public int TypeOfJob { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [Display(Name = "Speciella krav")]
        public bool SpecialRequirements { get; set; }

        [Display(Name = "Information")]
        public string SpecialRequirementsText { get; set; }

        [Display(Name = "Speciella önskemål")]
        public bool SpecialNeeds { get; set; }

        [Display(Name = "Information")]
        public string SpecialNeedsText { get; set; }
    }
}
