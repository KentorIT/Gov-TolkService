using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderRequirementModel
    {
        public int? OrderRequirementId { get; set; }

        [Required]
        [NoDisplayName]
        [Display(Name = "Typ")]
        public RequirementType? RequirementType { get; set; }

        public bool RequirementIsRequired { get; set; }

        [Display(Name = "Beskrivning")]
        [Required]
        [StringLength(1000)]
        public string RequirementDescription { get; set; }

        public string Answer { get; set; }

        public bool? CanSatisfyRequirement { get; set; }

        [ClientRequired]
        [NoDisplayName]
        public Gender? Gender { get; set; }
    }
}
