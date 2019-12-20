using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UserListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(UserId), Visible = false)]
        public int UserId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(Name), ColumnName = "NameFamily", SortOnWebServer = false, Title = "Namn")]
        public string Name { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(Email), Title = "E-postadress")]
        public string Email { get; set; }

        [ColumnDefinitions(Index = 3, Name = nameof(Organisation), Title = "Organisation")]
        public string Organisation { get; set; }

        [ColumnDefinitions(Index = 4, Name = nameof(LastLoginAt), SortOnWebServer = false, Title = "Senaste inloggning")]
        public string LastLoginAt { get; set; }

        public bool IsActive { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName { get => CssClassHelper.GetColorClassNameForItemStatus(IsActive); }
    }
}
