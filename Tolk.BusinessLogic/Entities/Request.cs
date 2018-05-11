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

        #region foreign keys

        public int RankingId { get; set; }

        public RequestStatus Status { get; set; }

        public Ranking Ranking { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        public string InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public AspNetUser Interpreter { get; set; }

        public DateTimeOffset? ModifiedDate { get; set; }

        public string ModifiedBy { get; set; }

        [ForeignKey(nameof(ModifiedBy))]
        public AspNetUser ModifyUser { get; set; }

        public string ImpersonatingModifier { get; set; }

        [ForeignKey(nameof(ImpersonatingModifier))]
        public AspNetUser ModifiedByImpersonator { get; set; }

        #endregion
    }
}
