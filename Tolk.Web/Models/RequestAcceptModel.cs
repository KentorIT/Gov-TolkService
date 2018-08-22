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
        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        public int InterpreterId { get; set; }

        public string NewInterpreterEmail { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public decimal? ExpectedTravelCosts { get; set; }

        public RequestStatus Status { get; set; }

        [Required]
        public InterpreterLocation? InterpreterLocation { get; set; }
    }
}
