using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class OrderAgreementModel
    {
        [ColumnDefinitions(Index = 0, Name = nameof(RequestId), Visible = false)]
        public int RequestId { get; set; }

        [Display(Name = "BokningsID")]
        [ColumnDefinitions(Index = 1, Name = nameof(OrderNumber), Title = "BokningsID")]
        public string OrderNumber { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(Index), Title = "Index")]
        [Display(Name = "Index")]
        public int Index { get; set; }

        [Display(Name = "Skapat")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Skapat av")]
        public string CreatedBy { get; set; }

        internal static OrderAgreementModel GetModelFromOrderAgreement(OrderAgreementPayload payload)
        {
            return new OrderAgreementModel
            {
                RequestId = payload.RequestId,
                Index = payload.Index,
                CreatedAt = payload.CreatedAt,
                OrderNumber = payload.Request.Order.OrderNumber,
                CreatedBy = payload.CreatedByUser?.FullName ?? "Systemet"
            };
        }
    }
}
