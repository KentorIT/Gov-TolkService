using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class Request
    {
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

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        public int? InterpreterId { get; set; }

        public int? CompetenceLevel { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }

        public DateTimeOffset? RecieveDate { get; set; }

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

        public DateTimeOffset? AcceptanceDate { get; set; }

        public int? AcceptanceBy { get; set; }

        [ForeignKey(nameof(AcceptanceBy))]
        public AspNetUser AcceptingUser { get; set; }

        public int? ImpersonatingAcceptanceBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAcceptanceBy))]
        public AspNetUser AcceptanceByImpersonator { get; set; }

        #region navigation

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        #endregion
    }
}
