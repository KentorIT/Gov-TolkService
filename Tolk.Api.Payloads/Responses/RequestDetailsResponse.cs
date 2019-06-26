using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class RequestDetailsResponse : ResponseBase
    {
        public string Status { get; set; }

        /// <summary>
        /// This includes any message connected to a cancellation or a denial
        /// </summary>
        public string StatusMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string OrderNumber { get; set; }
        public string Customer { get; set; }
        public string Region { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public LanguageModel Language { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public IEnumerable<LocationModel> Locations { get; set; }
        public IEnumerable<CompetenceModel> CompetenceLevels { get; set; }
        public bool CompetenceLevelsAreRequired { get; set; }
        public bool AllowExceedingTravelCost { get; set; }
        public string Description { get; set; }
        public string AssignentType { get; set; }
        public IEnumerable<AttachmentInformationModel> Attachments { get; set; }
        public IEnumerable<RequirementModel> Requirements { get; set; }
        public PriceInformationModel CalculatedPriceInformationFromRequest { get; set; }
        public PriceInformationModel CalculatedPriceInformationFromAnswer { get; set; }

        public InterpreterModel Interpreter { get; set; }
        public string InterpreterLocation { get; set; }
        public string InterpreterCompetenceLevel { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        public string ExpectedTravelCostInfo { get; set; }
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }

        #region requisition
        // RequisitionModel
        #endregion

        #region complaint
        // ComplaintModel
        #endregion
    }
}
