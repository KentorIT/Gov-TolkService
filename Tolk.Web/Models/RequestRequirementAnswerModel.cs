using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestRequirementAnswerModel
    {
        public int OrderRequirementId { get; set; }

        [NoDisplayName]
        public string Description { get; set; }

        [Display(Name = "Typ")]
        public RequirementType RequirementType { get; set; }

        [Display(Name = "Är ett krav")]
        public bool IsRequired { get; set; }

        [Display(Name = "Svar")]
        [SubItem]
        [StringLength(1000)]
        public string Answer { get; set; }

        [NoDisplayName]
        public bool CanMeetRequirement { get; set; }
    }
}
