using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class FailedWebHookCall
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FailedWebHookCallId { get; set; }
        public int OutboundWebHookCallId { get; set; }
        public OutboundWebHookCall OutboundWebHookCall { get; set; }
        public DateTimeOffset FailedAt { get; set; }
        public string ErrorMessage { get; set; }
    }
}
