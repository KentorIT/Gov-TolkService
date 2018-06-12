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
                BrokerName = requisition.Request.Ranking.BrokerRegion.Broker.Name,
                CustomerName = requisition.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = requisition.Request.Order.CustomerReferenceNumber,
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
                LanguageName = requisition.Request.Order.Language.Name,
                OrderNumber = requisition.Request.Order.OrderNumber.ToString(),
                RegionName = requisition.Request.Ranking.BrokerRegion.Region.Name,
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
