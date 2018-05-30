using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Data.Migrations;
using System.Linq;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class Request
    {
        private Request() { }

        public Request(Ranking ranking, DateTimeOffset expiry)
        {
            Ranking = ranking;
            Status = RequestStatus.Created;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? ExpectedTravelCosts { get; set; }

        public int RankingId { get; set; }

        public RequestStatus Status { get; set; }

        public Ranking Ranking { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        /// <summary>
        /// The time (inclusive) when the request is expired.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        public int? InterpreterId { get; set; }

        public int? CompetenceLevel { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }

        [MaxLength(1000)]
        public string DenyMessage { get; set; }

        public DateTimeOffset? RecievedAt { get; set; }

        public int? InterpreterLocation { get; set; }

        public int? ReceivedBy { get; set; }

        [ForeignKey(nameof(ReceivedBy))]
        public AspNetUser ReceivedByUser { get; set; }

        public int? ImpersonatingReceivedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingReceivedBy))]
        public AspNetUser ReceivedByImpersonator { get; set; }

        public DateTimeOffset? AnswerDate { get; set; }

        public int? AnsweredBy { get; set; }

        [ForeignKey(nameof(AnsweredBy))]
        public AspNetUser AnsweringUser { get; set; }

        public int? ImpersonatingAnsweredBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnsweredBy))]
        public AspNetUser AnsweredByImpersonator { get; set; }

        public DateTimeOffset? AnswerProcessedAt { get; set; }

        public int? AnswerProcessedBy { get; set; }

        [ForeignKey(nameof(AnswerProcessedBy))]
        public AspNetUser ProcessingUser { get; private set; }

        public int? ImpersonatingAnswerProcessedBy { get; set; }

        public void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId)
        {
            if(Status != RequestStatus.Created)
            {
                throw new InvalidOperationException($"Tried to mark request {RequestId} as received by {userId}({impersonatorId}) but it is already {Status}");
            }

            Status = RequestStatus.Received;
            RecievedAt = receiveTime;
            ReceivedBy = userId;
            ImpersonatingReceivedBy = impersonatorId;
        }

        [ForeignKey(nameof(ImpersonatingAnswerProcessedBy))]
        public AspNetUser AnswerProcessedByImpersonator { get; private set; }

        #region navigation

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        #endregion

        public void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if(Status != RequestStatus.Accepted)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only Accepted requests can be approved");
            }

            var approvedRequest = Order.Requests.FirstOrDefault(r => r.Status == RequestStatus.Approved);
            if(approvedRequest != null)
            {
                throw new InvalidOperationException($"Can only approve one request for an order. Order {OrderId} already has an approved request {approvedRequest.RequestId}.");
            }

            Status = RequestStatus.Approved;
            Order.Status = OrderStatus.ResponseAccepted;
            AnswerProcessedAt = approveTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
        }

        public void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            if(Status != RequestStatus.Accepted)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only Accepted requests can be denied.");
            }

            Status = RequestStatus.DeniedByCreator;
            AnswerProcessedAt = denyTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
        }
    }
}
