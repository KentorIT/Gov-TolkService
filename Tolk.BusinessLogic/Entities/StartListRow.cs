using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    /// <summary>
    /// NOTE: This is connected to the views CustomerStartListRows and BrokerStartListRows so it is not possible to change things here, without changing the view accordingly.
    /// </summary>
    /// 
    public class StartListRow
    {
        public StartListRowType RowType { get; set; }
        public string LanguageName { get; set; }
        public string OrderNumber { get; set; }
        public string OrderGroupNumber { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public TimeSpan? ExpectedLength { get; set; }
        public DateTimeOffset? RespondedStartAt { get; set; }
        public DateTimeOffset? CalculatedEndAt { get; set; }
        public RequisitionStatus? RequisitionStatus { get; set; }
        public ComplaintStatus? ComplaintStatus { get; set; }
        public DateTimeOffset EntityDate { get; set; }
        public int? CompetenceLevel { get; set; }
        public int? ExtraCompetencelevel { get; set; }
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }
        public DateTimeOffset? AnsweredAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? RequestExpiresAt { get; set; }
        public int? ReplacingOrderId { get; set; }
        public DateTimeOffset? LastRequestCreatedUpdatedAt { get; set; }
        public int? NoOfChildren { get; set; }
        public int? NoOfExtraInterpreter { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }
        public bool IsSingleOccasion => NoOfChildren == 1 || (NoOfChildren == 2 && HasExtraInterpreter);
        public bool HasExtraInterpreter => NoOfExtraInterpreter > 0;
    }
}
