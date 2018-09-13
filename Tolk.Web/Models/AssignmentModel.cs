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
    public class AssignmentModel
    {
        public int RequestId { get; set; }

        public int OrderId { get; set; }

        public int BrokerId { get; set; }

        [Display(Name = "AvropsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. moms)")]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation InterpreterLocation { get; set; }

        [Display(Name = "Typ av distanstolkning")]
        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        public string OffSiteContactInformation { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Adress")]
        public string Address { get; set; }

        [Display(Name = "Kund")]
        public string CustomerName { get; set; }

        [Display(Name = "Börjar")]
        public DateTimeOffset StartDateTime { get; set; }

        [Display(Name = "Tar slut")]
        public DateTimeOffset EndDateTime { get; set; }

        [Display(Name = "Förmedlat av")]
        public string BrokerName { get; set; }

        public int? RequisitionId { get; set; }

        public string ReplacedByOrderNumber { get; set; }

        public OrderStatus? ReplacedByOrderStatus{ get; set; }

        public string ReplacingOrderNumber { get; set; }

        public bool AllowRequisitionRegistration{ get; set; } = false;

        #region methods

        public static AssignmentModel GetModelFromRequest(Request request, DateTimeOffset timeNow)
        {
            int? requisitionId = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.Created || r.Status == RequisitionStatus.Approved)?.RequisitionId;

            return new AssignmentModel
            {
                OrderId = request.OrderId,
                OrderNumber = request.Order.OrderNumber.ToString(),
                ExpectedTravelCosts = request.ExpectedTravelCosts ?? 0,
                InterpreterLocation = (InterpreterLocation)request.InterpreterLocation.Value,
                Address = request.Order.CompactAddress,
                CustomerName = request.Order.CustomerOrganisation.Name,
                StartDateTime = request.Order.StartAt,
                EndDateTime = request.Order.EndAt,
                BrokerName = request.Ranking.Broker.Name,
                LanguageName = request.Order.OtherLanguage ?? request.Order.Language?.Name ?? "-",
                RequestId = request.RequestId,
                OffSiteAssignmentType = request.Order.OffSiteAssignmentType,
                OffSiteContactInformation = request.Order.OffSiteContactInformation,
                RequisitionId = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.Created || r.Status == RequisitionStatus.Approved)?.RequisitionId,
                AllowRequisitionRegistration = (request.Order.StartAt < timeNow && !requisitionId.HasValue && !request.Order.ReplacingOrderId.HasValue && request.Status == RequestStatus.Approved),
                ReplacedByOrderNumber = request.Order.ReplacedByOrder?.OrderNumber,
                ReplacedByOrderStatus = request.Order.ReplacedByOrder?.Status,
                ReplacingOrderNumber = request.Order.ReplacingOrder?.OrderNumber
            };
        }

        #endregion
    }
}
