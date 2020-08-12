using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class CreateOrderModel : ApiPayloadBaseModel
    {
        public DateTimeOffset? LatestAnswerBy { get; set; }

        [Required]
        public string AssignmentType { get; set; }

        [Required]
        public string Region { get; set; }

        [Required]
        public string Language { get; set; }

        public string Dialect { get; set; }

#warning does not have any solution on how to handle these
        public string CustomerUnit { get; set; }

#warning how to differ from Customer unit?
        public string UnitName { get; set; }

        [Required]
        public string InvoiceReference { get; set; }

#warning Needs a more publicly accepted name, and should require an email?
        public string ContactPerson { get; set; }

#warning needs api list
        public string CompetenceLevelDesireType { get; set; }

        public string CompetenceLevelFirst { get; set; }

        public string CompetenceLevelSecond { get; set; }

#warning needs api list
        public string AllowExceedingTravelCost { get; set; }

        public bool? IsCreatorInterpreterUser { get; set; }

#warning needs to handle attachments
        //public AttachmentListModel AttachmentListModel { get; set; }

        public string CustomerReferenceNumber { get; set; }

        public string Description { get; set; }

        public DateTimeOffset StartAt { get; }

        public DateTimeOffset EndAt { get; }

        #region extra requirements

        public List<RequirementModel> Requirements { get; set; }

        public List<RequirementModel> DesiredRequirements { get; set; }

        #endregion
    }
}
