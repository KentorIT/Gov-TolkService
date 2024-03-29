﻿using System;
using System.Collections.Generic;
using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class RequestGroupDetailsResponse : ResponseBase
    {
        public string Status { get; set; }

        /// <summary>
        /// This includes any message connected to a cancellation or a denial
        /// </summary>
        public string StatusMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string OrderGroupNumber { get; set; }
        public CustomerInformationModel CustomerInformation { get; set; }
        public string Region { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? LastAcceptAt { get; set; }
        public string RequiredAnswerLevel { get; set; }
        public string RequestAnswerRuleType { get; set; }
        public LanguageModel Language { get; set; }
        public IEnumerable<LocationModel> Locations { get; set; }
        public IEnumerable<CompetenceModel> CompetenceLevels { get; set; }
        public bool CompetenceLevelsAreRequired { get; set; }
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }
        public bool? CreatorIsInterpreterUser { get; set; }
        public string Description { get; set; }
        public string AssignmentType { get; set; }
        public IEnumerable<AttachmentInformationModel> Attachments { get; set; }
        public IEnumerable<RequirementModel> Requirements { get; set; }
        public IEnumerable<RequestDetailsResponse> Occasions { get; set; }
        public string BrokerReferenceNumber { get; set; }
    }
}
