using System;
using System.Collections.Generic;
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

        public string NewInterpreterFirstName { get; set; }
        public string NewInterpreterLastName { get; set; }
        public string NewInterpreterOfficialInterpreterId { get; set; }
        public string NewInterpreterPhoneNumber { get; set; }
        public string NewInterpreterEmail { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; } = new List<RequestRequirementAnswerModel>();

        public decimal? ExpectedTravelCosts { get; set; }

        public string ExpectedTravelCostInfo { get; set; }

        public RequestStatus Status { get; set; }

        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
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
