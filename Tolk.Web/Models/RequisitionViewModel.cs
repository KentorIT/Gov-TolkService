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

namespace Tolk.Web.Models
{
    public class RequisitionViewModel : RequisitionModel
    {
        [Display(Name = "Registrerad av")]
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
        public string LocationStreet { get; set; }

        [Display(Name = "Postnummer")]
        public string LocationZipCode { get; set; }

        [Display(Name = "Ort")]
        public string LocationCity { get; set; }

        [Display(Name = "Beställd kompetensnivå")]
        public CompetenceAndSpecialistLevel RequiredCompetenceLevel { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [Display(Name = "Beräknat pris inklusive förmedlingsavgift och ev. OB (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal CalculatedPrice { get; set; }

        [Display(Name = "Slutgiltigt pris inklusive tidsspillan, förmedlingsavgift och ev. OB (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ResultingPrice { get; set; }

        [Display(Name = "Meddelande vid nekande")]
        public string DenyMessage { get; set; }

        public DateTimeOffset StoredTimeWasteBeforeStartedAt { get; set; }

        public DateTimeOffset StoredTimeWasteAfterEndedAt { get; set; }

        [Display(Name = "Total registrerad tidsspillan")]
        public string TotalRegisteredWasteTime
        {
            get
            {
                var waste = (StoredTimeWasteAfterEndedAt - SessionEndedAt) + (SessionStartedAt - StoredTimeWasteBeforeStartedAt);
                return waste.Hours > 0 ? $"{waste.Hours} timmar {waste.Minutes} minuter" : $"{waste.Minutes} minuter";
            }
        }

        public bool AllowCreation {get;set;}

        #region methods

        public static RequisitionViewModel GetViewModelFromRequisition(Requisition requisition)
        {
            return new RequisitionViewModel
            {
                RequestId = requisition.RequestId,
                PreviousRequisitionId = requisition.Request.Requisitions.SingleOrDefault(r => r.ReplacedByRequisitionId == requisition.RequisitionId)?.RequisitionId,
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
                //TODO: Should be Name!
                InterpreterName = requisition.Request.Interpreter.User.Email,
                InterpreterLocation = (InterpreterLocation) requisition.Request.InterpreterLocation,
                OffSiteAssignmentType = requisition.Request.Order.OffSiteAssignmentType,
                OffSiteContactInformation = requisition.Request.Order.OffSiteContactInformation,
                LocationStreet = requisition.Request.Order.Street,
                LocationZipCode = requisition.Request.Order.ZipCode,
                LocationCity = requisition.Request.Order.City,
                LanguageName = requisition.Request.Order.OtherLanguage ?? requisition.Request.Order.Language.Name,
                RequiredCompetenceLevel = requisition.Request.Order.RequiredCompetenceLevel,
                AllowMoreThanTwoHoursTravelTime = requisition.Request.Order.AllowMoreThanTwoHoursTravelTime,
                OrderNumber = requisition.Request.Order.OrderNumber.ToString(),
                RegionName = requisition.Request.Ranking.Region.Name,
                //TODO: Should be Name!
                CreatedBy = requisition.CreatedByUser.Email,
                CreatedAt = requisition.CreatedAt,
                Message = requisition.Message,
                Status = requisition.Status,
                DenyMessage = requisition.DenyMessage,
            };
        }

        #endregion
    }
}
