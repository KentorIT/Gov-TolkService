using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class WebHookFilterModel
    {
        [Display(Name = "Status")]
        public WebhookStatus? Status { get; set; }

        [Display(Name = "Notifieringstyp")]
        public NotificationType? NotificationType { get; set; }

        public bool HasActiveFilters
        {
            get => NotificationType.HasValue;
        }

        internal IQueryable<OutboundWebHookCall> Apply(IQueryable<OutboundWebHookCall> items)
        {
            if (Status.HasValue)
            {
                switch (Status)
                {
                    case WebhookStatus.Failed:
                        items = items.Where(i => i.FailedTries >= 5 && i.ResentHookId == null);
                        break;
                    case WebhookStatus.Ongoing:
                        items = items.Where(i => i.DeliveredAt == null && i.FailedTries < 5 && i.ResentHookId == null);
                        break;
                    case WebhookStatus.Handled:
                        items = items.Where(i => i.ResentHookId != null);
                        break;
                    case WebhookStatus.Succeded:
                        items = items.Where(i => i.DeliveredAt != null);
                        break;
                }
            }

            items = NotificationType.HasValue ? items.Where(i => i.NotificationType == NotificationType) : items;

            return items;
        }
    }

    public enum WebhookStatus
    {
        [Description("Alla")]
        [Display(Name = "Alla")]
        [CustomName("all")]
        All = 0,
        [Description("Pågående")]
        [Display(Name = "Pågående")]
        [CustomName("ongoing")]
        Ongoing = 1,
        [Description("Misslyckade")]
        [Display(Name = "Misslyckade")]
        [CustomName("failed")]
        Failed = 2,
        [Description("Lyckade")]
        [Display(Name = "Lyckade")]
        [CustomName("succeded")]
        Succeded = 3,
        [Description("Hanterade")]
        [Display(Name = "Hanterade")]
        [CustomName("handled")]
        Handled = 4
    }
}
