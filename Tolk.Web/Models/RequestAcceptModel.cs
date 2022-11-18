using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestAcceptModel : IModel
    {
        public int RequestId { get; set; }

        [ClientRequired]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevelOnAccept { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public string BrokerReferenceNumber { get; set; }

        public List<FileModel> Files { get; set; }
    }
}
