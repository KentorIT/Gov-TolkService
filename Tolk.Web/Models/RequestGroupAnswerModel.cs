using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequestGroupAnswerModel : IModel
    {
        public int RequestGroupId { get; set; }

        public InterpreterLocation? InterpreterLocation { get; set; }

        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        public InterpreterAnswerModel InterpreterAnswerModel { get; set; }

        public InterpreterAnswerModel ExtraInterpreterAnswerModel { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public string BrokerReferenceNumber { get; set; }

        public List<FileModel> Files { get; set; }
    }
}
