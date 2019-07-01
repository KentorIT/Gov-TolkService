using System;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class EmailListItemModel
    {
        public int OutboundEmailId { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string DisplayBody => Body.Length > 100 ? Body.Substring(0, 100) + "..." : Body;

        public string Recipient { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        public bool IsSent => SentAt.HasValue;

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsSent);
    }
}
