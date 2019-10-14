using System;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class EmailListItemModel : EmailModel
    {
        [ColumnDefinitions(Index = 1, Name = nameof(CreatedAtDisplay), ColumnName = "CreatedAt", SortOnWebServer = false, Title = "Skapat")]
        public string CreatedAtDisplay => CreatedAt.ToSwedishString("yyyy-MM-dd HH:mm");

        [ColumnDefinitions(Index = 4, Name = nameof(DisplayBody), ColumnName = "PlainBody", SortOnWebServer = false, Title = "Innehåll")]
        public string DisplayBody => Body.Length > 100 ? Body.Substring(0, 100) + "..." : Body;

        [ColumnDefinitions(Index = 5, Name = nameof(SentAtDisplay), ColumnName = "DeliveredAt", SortOnWebServer = false, Title = "Skickat")]
        public string SentAtDisplay => SentAt?.ToSwedishString("yyyy-MM-dd HH:mm") ?? "-";

        [ColumnDefinitions(Index = 6, Name = nameof(ResentAtDisplay), Title = "Omskickat")]
        public string ResentAtDisplay => ResentAt?.ToSwedishString("yyyy-MM-dd HH:mm") ?? "-";

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(SentAt.HasValue);
    }
}
