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
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [ClientRequired]
        public int? InterpreterId { get; set; }

        public string NewInterpreterFirstName { get; set; }
        public string NewInterpreterLastName { get; set; }
        public string NewInterpreterOfficialInterpreterId { get; set; }
        public string NewInterpreterPhoneNumber { get; set; }
        public string NewInterpreterEmail { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public decimal? ExpectedTravelCosts { get; set; }

        public string ExpectedTravelCostInfo { get; set; }

        public string BrokerReferenceNumber { get; set; }

        public RequestStatus Status { get; set; }

        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        public List<FileModel> Files { get; set; }

        [ClientRequired]
        public InterpreterLocation? InterpreterLocation { get; set; }

        public InterpreterInformation GetNewInterpreterInformation()
        {
            return new InterpreterInformation
            {
                FirstName = NewInterpreterFirstName,
                LastName = NewInterpreterLastName,
                Email = NewInterpreterEmail,
                PhoneNumber = NewInterpreterPhoneNumber,
                OfficialInterpreterId = NewInterpreterOfficialInterpreterId
            };
        }
    }
}
