using System;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class EmailListItemModel : EmailModel
    {
        [ColumnDefinitions(Index = 1, Name = nameof(CreatedAtDisplay), Title = "Skapat")]
        public string CreatedAtDisplay => CreatedAt.ToString("yyyy-MM-dd HH:mm");

        [ColumnDefinitions(Index = 4, Name = nameof(DisplayBody), Title = "Innehåll")]
        public string DisplayBody => Body.Length > 100 ? Body.Substring(0, 100) + "..." : Body;

        [ColumnDefinitions(Index = 5, Name = nameof(SentAtDisplay), Title = "Skickat")]
        public string SentAtDisplay => SentAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";
        [ColumnDefinitions(Index = 6, Name = nameof(ResentAtDisplay), Title = "Omskickat")]
        public string ResentAtDisplay => ResentAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(SentAt.HasValue);
    }
}
