﻿using System;
using System.Collections.Generic;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestModel : WebHookPayloadBaseModel
    {
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
        public PriceInformationModel PriceInformation { get; set; }
    }
}


