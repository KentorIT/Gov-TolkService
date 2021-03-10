using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class InterpreterListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(Id), Visible = false)]
        public int Id { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(Name), ColumnName = "LastName", SortOnWebServer = false, Title = "Namn")]
        public string Name { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(Email), Title = "E-postadress")]
        public string Email { get; set; }

        [ColumnDefinitions(Index = 3, Name = nameof(OfficialInterpreterId), Title = "Kammarkollegiets tolknummer")]
        public string OfficialInterpreterId { get; set; }

        public bool IsActive { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);
    }
}
