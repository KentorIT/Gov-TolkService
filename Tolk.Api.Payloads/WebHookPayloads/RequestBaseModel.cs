﻿using System;
using System.Collections.Generic;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestBaseModel : WebHookPayloadBaseModel
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string Region { get; set; }
        public CustomerInformationModel CustomerInformation { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? LastAcceptAt { get; set; }
        public LanguageModel Language { get; set; }
        public IEnumerable<LocationModel> Locations { get; set; }
        public IEnumerable<CompetenceModel> CompetenceLevels { get; set; }
        public bool CompetenceLevelsAreRequired { get; set; }
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }
        public bool? CreatorIsInterpreterUser { get; set; }
        public string AssignmentType { get; set; }
        public string Description { get; set; }
        public IEnumerable<AttachmentInformationModel> Attachments { get; set; }
        public IEnumerable<RequirementModel> Requirements { get; set; }
        public string RequiredAnswerLevel{ get; set; }
        public string RequestAnswerRuleType { get; set; }
    }
}


