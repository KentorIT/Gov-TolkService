using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroup : RequestBase
    {
        #region constructors

        private RequestGroup() { }

        public RequestGroup(Ranking ranking, DateTimeOffset? expiry, DateTimeOffset creationTime, List<Request> requests, bool isTerminalRequest = false)
        {
            requests.ForEach(r => r.RequestGroup = this);
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
            CreatedAt = creationTime;
            IsTerminalRequest = isTerminalRequest;
            Requests = requests;
        }

        #endregion

        #region properties

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestGroupId { get; set; }

        public int OrderGroupId { get; set; }

        [ForeignKey(nameof(OrderGroupId))]
        public OrderGroup OrderGroup { get; set; }

        #endregion

        #region navigation

        public List<Request> Requests { get; set; }

        #endregion

        #region Methods

        public void SetStatus(RequestStatus status)
        {
            Status = status;
            Requests.ForEach(r => r.Status = status);
        }

        #endregion

        #region private methods

        #endregion
    }
}
