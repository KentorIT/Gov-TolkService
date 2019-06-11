using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Enums;
using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class FaqListItemModel
    {
        public int FaqId { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public bool IsDisplayed { get; set; }

        public IEnumerable<DisplayUserRole> DisplayedFor { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsDisplayed);
    }
}
