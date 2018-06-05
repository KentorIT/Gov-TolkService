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
    public class RequestAcceptModel
    {
        public int RequestId { get; set; }

        [Required]
        [Display(Name = "Kompetensnivå")]
        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolk")]
        public int? InterpreterId { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. moms)")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Required]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Svar senast")]
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
