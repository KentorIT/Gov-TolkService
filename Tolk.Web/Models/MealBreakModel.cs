using System;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class MealBreakModel
    {

        [Required]
        [Display(Name = "Startid för måltidspaus")]
        public DateTime MealBreakStartAt { get; set; }

        [Required]
        [Display(Name = "Sluttid för måltidspaus")]
        public DateTime MealBreakEndAt { get; set; }

    }
}
