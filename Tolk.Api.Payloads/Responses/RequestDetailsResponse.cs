﻿using System;
using System.Collections.Generic;
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
        public string BrokerReferenceNumber { get; set; }
        public CustomerInformationModel CustomerInformation { get; set; }
        public string Region { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? LastAcceptAt { get; set; }

        public string RequiredAnswerLevel { get; set; }
        public string RequestAnswerRuleType { get; set; }
        public LanguageModel Language { get; set; }
        public DateTimeOffset? FlexibleStartAt { get; set; }
        public DateTimeOffset? FlexibleEndAt { get; set; }
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
        public TimeSpan? ExpectedLength { get; set; }
        public bool IsFlexibleRequest { get; set; }
        public IEnumerable<LocationModel> Locations { get; set; }
        public IEnumerable<CompetenceModel> CompetenceLevels { get; set; }
        public bool CompetenceLevelsAreRequired { get; set; }
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }
        public bool? CreatorIsInterpreterUser { get; set; }
        public bool? MealBreakIncluded { get; set; }
        public string Description { get; set; }
        public string AssignmentType { get; set; }
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
