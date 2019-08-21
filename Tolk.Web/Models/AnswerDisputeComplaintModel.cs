using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class AnswerDisputeComplaintModel
    {
        public int ComplaintId { get; set; }

        [StringLength(1000)]
        public string AnswerDisputedMessage { get; set; }

        [StringLength(1000)]
        public string RefuteMessage { get; set; }
    }
}
