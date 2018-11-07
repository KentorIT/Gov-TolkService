namespace Tolk.BusinessLogic.Utilities
{
    public class BrokerNotificationSettings
    {
        public bool SendEmail { get; set; }

        public bool CallWebhook { get; set; }

        public string EmailAddress { get; set; }

        public string Webhook { get; set; }

        public int RecipientUserId { get; set; } 
    }
}
