using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class PeppolMessageModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(PeppolPayloadId), Visible = false)]
        public int PeppolPayloadId { get; set; }

        [Display(Name = "Är senaste")]
        [ColumnDefinitions(Index = 1, Name = nameof(IsLatest), Visible = false)]
        public bool IsLatest { get; set; }

        [Display(Name = "Identifierare")]
        [ColumnDefinitions(Index = 2, Name = nameof(IdentificationNumber), Title = "Identifierare")]
        public string IdentificationNumber { get; set; }

        [Display(Name = "Typ")]
        [ColumnDefinitions(Index = 3, Name = nameof(PeppolMessageType), Title = "Typ")]
        public string PeppolMessageType { get; set; }
        [Display(Name = "BokningsID")]
        [ColumnDefinitions(Index = 4, Name = nameof(OrderNumber), Title = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Skapat")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Skapat av")]
        [ColumnDefinitions(Index = 6, Name = nameof(CreatedBy), Title = "Skapad av", Sortable = false)]
        public string CreatedBy { get; set; }

        [ColumnDefinitions(Index = 7, Name = nameof(CustomerName), Title = "Myndighet", Sortable = false)]
        public string CustomerName { get; set; }

        [Display(Name = "Genererad från")]
        public string BasedOn { get; set; }

        public bool EnableMockInvoice { get; set; }
        public int RequestId { get; set; }
        internal static PeppolMessageModel GetModelFromPeppolPayload(PeppolPayload payload, bool EnableMockInvoice)
        {
            return new PeppolMessageModel
            {
                PeppolPayloadId = payload.PeppolPayloadId,
                IdentificationNumber = payload.IdentificationNumber,
                CreatedAt = payload.CreatedAt,
                PeppolMessageType = payload.PeppolMessageType.ToString(),
                OrderNumber = payload.Request.Order.OrderNumber,
                CreatedBy = payload.CreatedByUser?.FullName ?? "Systemet",
                BasedOn = payload.RequisitionId.HasValue ? "Genererad från rekvisition" : "Genererad från bekräftelse",
                IsLatest = !payload.ReplacedById.HasValue,
                EnableMockInvoice = EnableMockInvoice,
                RequestId = payload.RequestId
            };
        }
    }
}
