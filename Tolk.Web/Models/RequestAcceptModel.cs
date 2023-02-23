using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestAcceptModel : IModel
    {
        public int RequestId { get; set; }

        [Required]
        public InterpreterLocation InterpreterLocationOnAccept { get; set; }

        [ClientRequired]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevelOnAccept { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public string BrokerReferenceNumber { get; set; }

        public List<FileModel> Files { get; set; }

        //TODO: Make a lot of inheritence here!!!
        public TimeSpan? EarliestStartAt { get; set; }
        public TimeSpan? LatestStartAt { get; set; }

        public bool IsFlexibleOrder { get; set; }

        [RequiredIf(nameof(IsFlexibleOrder), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        [ValidTimeSpanRange(StartAtProperty = nameof(EarliestStartAt), EndAtProperty = nameof(LatestStartAt))]
        public TimeSpan? RespondedStartAt { get; set; }

    }
}
