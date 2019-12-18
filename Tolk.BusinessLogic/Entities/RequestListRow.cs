using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    /// <summary>
    /// NOTE: This is connected to the view RequestListRows, so it is not possible to change things here, without changing the view accordingly.
    /// </summary>
    public class RequestListRow
    {
        public int EntityId { get; set; }
        public OrderRowType RowType { get; set; }
        public string LanguageName { get; set; }
        public int? LanguageId { get; set; }
        public string EntityNumber { get; set; }
        public string EntityParentNumber { get; set; }
        public RequestStatus Status { get; set; }
        public string RegionName { get; set; }
        public int RegionId { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public int? AnsweredBy { get; set; }
        public int? BrokerId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string CustomerName { get; set; }
        public int CustomerOrganisationId { get; set; }
        public string CustomerReferenceNumber { get; set; }
    }
}
