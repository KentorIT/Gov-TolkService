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
    public class RequisitionProcessModel : RequisitionViewModel
    {
        public int RequisitionId { get; set; }

        public static RequisitionProcessModel GetProcessViewModelFromRequisition(Requisition requisition)
        {
            return new RequisitionProcessModel
            {
                PreviousRequisition = requisition.Request.Requisitions.SingleOrDefault(r => r.ReplacedByRequisitionId == requisition.RequisitionId),
                RequisitionId = requisition.RequisitionId,
                BrokerName = requisition.Request.Ranking.Broker.Name,
                CustomerOrganizationName = requisition.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = requisition.Request.Order.CustomerReferenceNumber,
                ExpectedEndedAt = requisition.Request.Order.EndAt,
                ExpectedStartedAt = requisition.Request.Order.StartAt,
                SessionEndedAt = requisition.SessionEndedAt,
                SessionStartedAt = requisition.SessionStartedAt,
                ExpectedTravelCosts = requisition.Request.ExpectedTravelCosts ?? 0,
                TravelCosts = requisition.TravelCosts,
                StoredTimeWasteBeforeStartedAt = requisition.TimeWasteBeforeStartedAt ?? requisition.SessionStartedAt,
                StoredTimeWasteAfterEndedAt = requisition.TimeWasteAfterEndedAt ?? requisition.SessionEndedAt,
                InterpreterName = requisition.Request.Interpreter.User.CompleteContactInformation,
                LanguageName = requisition.Request.Order.OtherLanguage ?? requisition.Request.Order.Language?.Name ?? "-",
                OrderNumber = requisition.Request.Order.OrderNumber.ToString(),
                RegionName = requisition.Request.Ranking.Region.Name,
                OrderCreatedBy = requisition.Request.Order.CreatedByUser.CompleteContactInformation,
                RequisitionCreatedBy = requisition.CreatedByUser.CompleteContactInformation,
                CreatedAt = requisition.CreatedAt,
                Message = requisition.Message,
                Status = requisition.Status
            };
        }

    }
}
