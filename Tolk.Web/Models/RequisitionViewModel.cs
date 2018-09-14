using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequisitionViewModel : RequisitionModel
    {
        [Display(Name = "Registrerad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }

        [Display(Name = "Registrerad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Status")]
        public RequisitionStatus Status { get; set; }

        [Display(Name = "Annan kontaktperson")]
        public int? ContactPersonId { get; set; }

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

        [Display(Name = "Typ av distanstolkning")]
        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        public string OffSiteContactInformation { get; set; }

        [Display(Name = "Adress")]
        public string Address { get; set; }

        [Display(Name = "Krav på kompetensnivå")]
        public bool SpecificCompetenceLevelRequired { get; set; }

        [Display(Name = "Kravad kompetensnivå")]
        public CompetenceAndSpecialistLevel? RequiredCompetenceLevelFirst { get; set; }

        [Display(Name = "Alternativ kravad kompetensnivå")]
        public CompetenceAndSpecialistLevel? RequiredCompetenceLevelSecond { get; set; }

        [Display(Name = "Önskad kompetensnivå (förstahand)")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [Display(Name = "Önskad kompetensnivå (andrahand)")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }

        [Display(Name = "Önskad kompetensnivå (tredjehand)")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelThird { get; set; }

        [Display(Name = "Tolkens faktiska kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpretersCompetenceLevel { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [Display(Name = "Meddelande vid nekande")]
        public string DenyMessage { get; set; }

        public DateTimeOffset StoredTimeWasteBeforeStartedAt { get; set; }

        public DateTimeOffset StoredTimeWasteAfterEndedAt { get; set; }

        [Display(Name = "Total registrerad tidsspillan")]
        public TimeSpan TotalRegisteredWasteTime
        {
            get
            {
                return (StoredTimeWasteAfterEndedAt - SessionEndedAt) + (SessionStartedAt - StoredTimeWasteBeforeStartedAt);
            }
        }

        public bool AllowCreation {get;set;}

        #region methods

        public static RequisitionViewModel GetViewModelFromRequisition(Requisition requisition)
        {
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
                RequestId = requisition.RequestId,
                PreviousRequisition = requisition.Request.Requisitions.SingleOrDefault(r => r.ReplacedByRequisitionId == requisition.RequisitionId),
                ReplacingRequisitionId = requisition.ReplacedByRequisitionId,
                BrokerName = requisition.Request.Ranking.Broker.Name,
                CustomerName = requisition.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = requisition.Request.Order.CustomerReferenceNumber,
                Description = requisition.Request.Order.Description,
                AssignmentType = requisition.Request.Order.AssignentType,
                UnitName = requisition.Request.Order.UnitName,
                ExpectedEndedAt = requisition.Request.Order.EndAt,
                ExpectedStartedAt = requisition.Request.Order.StartAt,
                SessionEndedAt = requisition.SessionEndedAt,
                SessionStartedAt = requisition.SessionStartedAt,
                ExpectedTravelCosts = requisition.Request.ExpectedTravelCosts ?? 0,
                TravelCosts = requisition.TravelCosts,
                StoredTimeWasteBeforeStartedAt = requisition.TimeWasteBeforeStartedAt ?? requisition.SessionStartedAt,
                StoredTimeWasteAfterEndedAt = requisition.TimeWasteAfterEndedAt ?? requisition.SessionEndedAt,
                InterpreterName = requisition.Request.Interpreter.User.CompleteContactInformation,
                InterpreterLocation = (InterpreterLocation)requisition.Request.InterpreterLocation,
                InterpretersCompetenceLevel = (CompetenceAndSpecialistLevel?)requisition.Request.CompetenceLevel,
                Address = $"{location.Street}\n{location.ZipCode} {location.City}",
                OffSiteAssignmentType = location.OffSiteAssignmentType,
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
                CreatedBy = requisition.CreatedByUser.CompleteContactInformation,
                CreatedAt = requisition.CreatedAt,
                Message = requisition.Message,
                Status = requisition.Status,
                DenyMessage = requisition.DenyMessage,
            };
        }

        #endregion
    }
}
