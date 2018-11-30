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
    public class RequestRequirementAnswerModel
    {
        public int OrderRequirementId { get; set; }
        [Display(Name = "Beställt behov")]
        public string Description { get; set; }

        [Display(Name = "Typ av behov")]
        public RequirementType RequirementType { get; set; }

        [Display(Name = "Är ett krav")]
        public bool IsRequired{ get; set; }

        [Display(Name = "Svar")]
        public string Answer { get; set; }

        [Display(Name = "Kan uppfylla behovet")]
        public bool CanMeetRequirement { get; set; }
    }
}
