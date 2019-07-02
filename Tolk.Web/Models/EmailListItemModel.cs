using System;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class EmailListItemModel : EmailModel
    {

        public string DisplayBody => Body.Length > 100 ? Body.Substring(0, 100) + "..." : Body;

        public bool IsSent => SentAt.HasValue;

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsSent);
    }
}
