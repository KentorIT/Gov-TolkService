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
    public class RequestAcceptModel
    {
        public int RequestId { get; set; }

        [ClientRequired]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [ClientRequired]
        public int? InterpreterId { get; set; }

        public string NewInterpreterEmail { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public decimal? ExpectedTravelCosts { get; set; }

        public RequestStatus Status { get; set; }

        public List<FileModel> Files { get; set; }

        [ClientRequired]
        public InterpreterLocation? InterpreterLocation { get; set; }
    }
}
