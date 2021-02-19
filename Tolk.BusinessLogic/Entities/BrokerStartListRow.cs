using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    /// <summary>
    /// NOTE: This is connected to the view BrokerStartListRows, so it is not possible to change things here, without changing the view accordingly.
    /// </summary>
    /// 

    public class BrokerStartListRow : StartListRow
    {
        public int? RequestId { get; set; }
        public int BrokerId { get; set; }
        public string ViewedBy { get; set; }
        public int? ViewedByUserId { get; set; }
        public RequestStatus? RequestStatus { get; set; }
        public RequestStatus? RequestGroupStatus { get; set; }
        public string CustomerName { get; set; }
        public int? RequestGroupId { get; set; }
        public DateTimeOffset? OrderChangedAt { get; set; }
        public DateTimeOffset? AnswerProcessedAt { get; set; }
        public bool RequestIsToBeProcessedByBroker => RequestStatus.HasValue && RequestStatus == Enums.RequestStatus.Created || RequestStatus == Enums.RequestStatus.Received;
        public bool RequestGroupIsToBeProcessedByBroker => RequestGroupStatus.HasValue && RequestGroupStatus == Enums.RequestStatus.Created || RequestGroupStatus == Enums.RequestStatus.Received;
    }
}

