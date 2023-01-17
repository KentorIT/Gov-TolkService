using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestNotificationWebhook
    {
        public int RequestNotificationId { get; set; }

        [ForeignKey(nameof(RequestNotificationId))]
        public RequestNotification RequestNotification { get; set; }

        public int OutboundWebhookId { get; set; }

        [ForeignKey(nameof(OutboundWebhookId))]
        public OutboundWebHookCall OutboundWebhook { get; set; }
    }
}
