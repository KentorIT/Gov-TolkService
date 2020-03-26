using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderDesiredRequirementModel
    {
        public int? UserDefaultSettingOrderRequirementId { get; set; }

        [Required]
        [NoDisplayName]
        [Display(Name = "Typ")]
        public RequirementType? DesiredRequirementType { get; set; }

        [Display(Name = "Beskrivning")]
        [Required]
        [StringLength(1000)]
        public string DesiredRequirementDescription { get; set; }
        [ClientRequired]
        [NoDisplayName]
        public Gender? DesiredGender { get; set; }
    }
}
