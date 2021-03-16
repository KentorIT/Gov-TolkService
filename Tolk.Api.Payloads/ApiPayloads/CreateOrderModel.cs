using NJsonSchema.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    public class CreateOrderModel : ApiPayloadBaseModel
    {
        public DateTimeOffset? LatestAnswerBy { get; set; }
        [Required]
        public string AssignmentType { get; set; }
        [Required]
        public string Region { get; set; }
        public string Language { get; set; }
        public string OtherLanguage { get; set; }
        public string Dialect { get; set; }
        public string CustomerUnit { get; set; }
        public string DepartmentName { get; set; }
        public string UserThatMayControlRequisition { get; set; }
        [Required]
        public string InvoiceReference { get; set; }
        public string AllowExceedingTravelCost { get; set; }
        public bool CreatorIsInterpreterUser { get; set; }
        public string CustomerReferenceNumber { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public bool MealBreakIncluded { get; set; }
        public bool CompetenceLevelsAreRequired { get; set; }
        public IEnumerable<LocationModel> Locations { get; set; }
        public IEnumerable<CompetenceModel> CompetenceLevels { get; set; }
        public IEnumerable<AttachmentModel> Attachments { get; set; }
        public IEnumerable<RequirementRequestModel> Requirements { get; set; }
    }
}
