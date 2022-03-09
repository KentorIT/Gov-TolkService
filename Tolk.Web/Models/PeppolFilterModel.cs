using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class PeppolFilterModel
    {
        [Display(Name = "Status")]
        public WebhookStatus? Status { get; set; }

        [Display(Name = "Notifieringstyp")]
        public NotificationType? NotificationType { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerId { get; set; }

        internal IQueryable<OutboundPeppolMessage> Apply(IQueryable<OutboundPeppolMessage> items)
        {
            if (Status.HasValue)
            {
                switch (Status)
                {
                    case WebhookStatus.Failed:
                        items = items.Where(i => i.FailedTries >= 5 && i.ReplacedByMessage == null);
                        break;
                    case WebhookStatus.Ongoing:
                        items = items.Where(i => i.DeliveredAt == null && i.FailedTries < 5 && i.ReplacedByMessage == null);
                        break;
                    case WebhookStatus.Handled:
                        items = items.Where(i => i.ReplacedByMessage != null);
                        break;
                    case WebhookStatus.Succeded:
                        items = items.Where(i => i.DeliveredAt != null);
                        break;
                }
            }

            items = NotificationType.HasValue ? items.Where(i => i.NotificationType == NotificationType) : items;
            items = CustomerId.HasValue ? items.Where(w => w.OrderAgreementPayload.Request.Order.CustomerOrganisationId == CustomerId) : items;

            return items;
        }
    }
}
