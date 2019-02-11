using System;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class SystemMessageListItemModel
    {

        public int SystemMessageId { get; set; }

        public SystemMessageType SystemMessageType { get; set; }

        public string SystemMessageHeader { get; set; }

        public DateTimeOffset ActiveFrom { get; set; }

        public DateTimeOffset ActiveTo { get; set; }

        public SystemMessageUserTypeGroup DisplayedFor { get; set; }

        public DateTimeOffset CreatedLastUpdatedAt { get; set; }

        public string CreatedLastUpdatedBy { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForSystemMessageType(SystemMessageType); }
    }
}
