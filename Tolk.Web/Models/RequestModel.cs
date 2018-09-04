﻿using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class RequestModel
    {
        public int RequestId { get; set; }

        [Display(Name = "Avropets status")]
        public RequestStatus Status { get; set; }

        public int BrokerId { get; set; }

        public OrderModel OrderModel { get; set; }

        public int? OrderId
        {
            get
            {
                return OrderModel?.OrderId;
            }
        }

        [Display(Name = "Orsak till avslag")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string DenyMessage { get; set; }


        [Display(Name = "Orsak till avbokning")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string CancelMessage { get; set; }

        [Required]
        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolk")]
        public int? InterpreterId { get; set; }

        [Required]
        [Display(Name = "Tolkens e-postadress")]
        public string NewInterpreterEmail { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        public int? RequisitionId { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. restid och moms)")]
        [DataType(DataType.Currency)]
        public decimal? ExpectedTravelCosts { get; set; }
        
        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Inställelsesätt enl. svar")]
        public InterpreterLocation? InterpreterLocationAnswer
        {
            get; set;
        }

        [Display(Name = "Svar senast")]
        public DateTimeOffset? ExpiresAt { get; set; }

        public bool AllowInterpreterChange { get; set; } = false;

        public bool AllowCancellation { get; set; } = false;

        #region view stuff

        [Display(Name = "Tillsatt tolk")]
        public string Interpreter{ get; set; }

        [Display(Name = "Beräknat pris inklusive förmedlingsavgift och ev. OB (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal? CalculatedPrice { get; set; }

        public int? ComplaintId { get; set; }

        [Display(Name = "Reklamationens status")]
        public ComplaintStatus? ComplaintStatus { get; set; }

        [Display(Name = "Typ av reklamation")]
        public ComplaintType? ComplaintType { get; set; }
        [Display(Name = "Reklamationens beskriving")]
        public string ComplaintMessage { get; set; }

        #endregion

        #region methods

        public static RequestModel GetModelFromRequest(Request request)
        {
            var complaint = request.Complaints?.FirstOrDefault();
            return new RequestModel
            {
                Status = request.Status,
                DenyMessage = request.DenyMessage,
                CancelMessage = request.CancelMessage,
                RequestId = request.RequestId,
                ExpiresAt = request.ExpiresAt,
                Interpreter = request.Interpreter?.User.UserName,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExpectedTravelCosts = request.ExpectedTravelCosts ?? 0,
                RequisitionId = request.Requisitions?.FirstOrDefault(req => req.Status == RequisitionStatus.Created || req.Status == RequisitionStatus.Approved)?.RequisitionId,
                RequirementAnswers = request.Order.Requirements.Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = r.IsRequired,
                    Requirement = $"{(r.IsRequired ? "Krav: " : "Önskemål: ")}{EnumHelper.GetDescription(r.RequirementType)} - {r.Description}",
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),
                InterpreterLocation = request.Order.InterpreterLocations.Count() == 1 ? request.Order.InterpreterLocations.Single()?.InterpreterLocation : null,
                OrderModel = OrderModel.GetModelFromOrder(request.Order),
                ComplaintId = complaint?.ComplaintId,
                ComplaintMessage = complaint?.ComplaintMessage,
                ComplaintStatus = complaint?.Status,
                ComplaintType = complaint?.ComplaintType
            };
        }

        #endregion
    }
}
