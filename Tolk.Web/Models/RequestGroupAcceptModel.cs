using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequestGroupAcceptModel
    {
        public int RequestGroupId { get; set; }

        [Required]
        public InterpreterLocation InterpreterLocationOnAccept { get; set; }

        public InterpreterAcceptModel InterpreterAcceptModel { get; set; }

        public InterpreterAcceptModel ExtraInterpreterAcceptModel { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public string BrokerReferenceNumber { get; set; }

        public List<FileModel> Files { get; set; }

    }
}
