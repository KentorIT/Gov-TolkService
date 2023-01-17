using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestNotificationEmail
    {
        public int RequestNotificationId { get; set; }

        [ForeignKey(nameof(RequestNotificationId))]
        public RequestNotification RequestNotification { get; set; }

        public int OutboundEmailId { get; set; }

        [ForeignKey(nameof(OutboundEmailId))]
        public OutboundEmail OutboundEmail { get; set; }
    }
}
