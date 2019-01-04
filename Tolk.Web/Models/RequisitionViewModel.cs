using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequisitionViewModel : RequisitionModel
    {
        public int RequisitionId { get; set; }

        [Display(Name = "Rekvisition registrerad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Status")]
        public RequisitionStatus Status { get; set; }

        [Display(Name = "Annan kontaktperson")]
        [DataType(DataType.MultilineText)]
        public string ContactPerson { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Beskrivning", Description = "Extra information om uppdraget i det fall det behövs")]
        public string Description { get; set; }

        [Display(Name = "Typ av tolkuppdrag")]
        [Required]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Enhet/avdelning")]
        public string UnitName { get; set; }

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        public string OffSiteContactInformation { get; set; }

        [Display(Name = "Adress")]
        public string Address { get; set; }

        [Display(Name = "Krav på kompetensnivå")]
        public bool SpecificCompetenceLevelRequired { get; set; }

        [Display(Name = "Krav på kompetensnivå")]
        public CompetenceAndSpecialistLevel? RequiredCompetenceLevelFirst { get; set; }

        [Display(Name = "Alternativt krav på kompetensnivå")]
        public CompetenceAndSpecialistLevel? RequiredCompetenceLevelSecond { get; set; }

        [Display(Name = "Önskad kompetensnivå (förstahand)")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [Display(Name = "Önskad kompetensnivå (andrahand)")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }

        [Display(Name = "Önskad kompetensnivå (tredjehand)")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelThird { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [Display(Name = "Anledning till underkännande av rekvisition")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string DenyMessage { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public bool AllowCreation { get; set; }

        public bool AllowProcessing { get; set; }

        [Display(Name = "Total summa")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get => ResultPriceInformationModel.TotalPriceToDisplay; }

        public EventLogModel EventLog { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequisitionStatus(Status); }

        #region methods

        public static RequisitionViewModel GetViewModelFromRequisition(Requisition requisition)
        {
            if (requisition == null)
            {
                return null;
            }
            var competenceLevels = requisition.Request.Order.CompetenceRequirements
                .Select(item => new OrderCompetenceRequirement
                {
                    CompetenceLevel = item.CompetenceLevel,
                    Rank = item.Rank,
                }).ToList();
            if (!requisition.Request.Order.SpecificCompetenceLevelRequired)
            {
                competenceLevels = competenceLevels.OrderBy(l => l.Rank).ToList();
            }
            var competenceFirst = competenceLevels.Count > 0 ? competenceLevels[0] : null;
            var competenceSecond = competenceLevels.Count > 1 ? competenceLevels[1] : null;
            var competenceThird = competenceLevels.Count > 2 ? competenceLevels[2] : null;
            var location = requisition.Request.Order.InterpreterLocations.Single(l => (int)l.InterpreterLocation == requisition.Request.InterpreterLocation.Value);
            return new RequisitionViewModel
            {
                RequisitionId = requisition.RequisitionId,
                RequestId = requisition.RequestId,
                PreviousRequisition = PreviousRequisitionViewModel.GetViewModelFromPreviousRequisition(requisition.Request.Requisitions.SingleOrDefault(r => r.ReplacedByRequisitionId == requisition.RequisitionId)),
                ReplacingRequisitionId = requisition.ReplacedByRequisitionId,
                BrokerName = requisition.Request.Ranking.Broker.Name,
                BrokerOrganizationnumber = requisition.Request.Ranking.Broker.OrganizationNumber,
                CustomerOrganizationName = requisition.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = requisition.Request.Order.CustomerReferenceNumber,
                Description = requisition.Request.Order.Description,
                AssignmentType = requisition.Request.Order.AssignentType,
                UnitName = requisition.Request.Order.UnitName,
                ExpectedEndedAt = requisition.Request.Order.EndAt,
                ExpectedStartedAt = requisition.Request.Order.StartAt,
                SessionEndedAt = requisition.SessionEndedAt,
                SessionStartedAt = requisition.SessionStartedAt,
                ExpectedTravelCosts = requisition.Request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                TravelCosts = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                TimeWasteTotalTime = requisition.TimeWasteTotalTime,
                TimeWasteIWHTime = requisition.TimeWasteIWHTime,
                Interpreter = requisition.Request.Interpreter.CompleteContactInformation,
                InterpreterLocation = (InterpreterLocation)requisition.Request.InterpreterLocation,
                InterpreterTaxCard = requisition.InterpretersTaxCard,
                Address = $"{location.Street}, {location.City}",
                OffSiteContactInformation = location.OffSiteContactInformation,
                LanguageName = requisition.Request.Order.OtherLanguage ?? requisition.Request.Order.Language?.Name ?? "-",
                SpecificCompetenceLevelRequired = requisition.Request.Order.SpecificCompetenceLevelRequired,
                RequiredCompetenceLevelFirst = requisition.Request.Order.SpecificCompetenceLevelRequired ? competenceFirst?.CompetenceLevel : null,
                RequiredCompetenceLevelSecond = requisition.Request.Order.SpecificCompetenceLevelRequired ? competenceSecond?.CompetenceLevel : null,
                RequestedCompetenceLevelFirst = requisition.Request.Order.SpecificCompetenceLevelRequired ? null : competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = requisition.Request.Order.SpecificCompetenceLevelRequired ? null : competenceSecond?.CompetenceLevel,
                RequestedCompetenceLevelThird = requisition.Request.Order.SpecificCompetenceLevelRequired ? null : competenceThird?.CompetenceLevel,
                AllowMoreThanTwoHoursTravelTime = requisition.Request.Order.AllowMoreThanTwoHoursTravelTime,
                OrderNumber = requisition.Request.Order.OrderNumber.ToString(),
                RegionName = requisition.Request.Ranking.Region.Name,
                OrderCreatedBy = requisition.Request.Order.CreatedByUser.CompleteContactInformation,
                RequisitionCreatedBy = requisition.CreatedByUser.FullName,
                CreatedAt = requisition.CreatedAt,
                Message = requisition.Message,
                Status = requisition.Status,
                DenyMessage = requisition.DenyMessage,
                ContactPerson = requisition.Request.Order.ContactPersonUser?.CompleteContactInformation,
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    DisplayFiles = requisition.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                }
            };
        }

        #endregion
    }
}
