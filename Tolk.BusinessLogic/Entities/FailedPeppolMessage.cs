using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class FailedPeppolMessage
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FailedPeppolMessageId { get; set; }
        public int OutboundPeppolMessageId { get; set; }

        [ForeignKey(nameof(OutboundPeppolMessageId))]
        public OutboundPeppolMessage OutboundPeppolMessage { get; set; }
        public DateTimeOffset FailedAt { get; set; }
        public string ErrorMessage { get; set; }
    }
}
