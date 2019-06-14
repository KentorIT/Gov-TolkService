using System;
using System.Collections.Generic;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class FaqListItemModel
    {
        public int FaqId { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public string DisplayListAnswer => Answer.Length > 100 ? Answer.Substring(0, 100) + "..." : Answer;

        public bool IsDisplayed { get; set; }

        public IEnumerable<DisplayUserRole> DisplayedFor { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsDisplayed);
    }
}
