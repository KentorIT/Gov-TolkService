using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderAgreementListItemModel : OrderAgreementModel
    {
        [ColumnDefinitions(Index = 5, Name = nameof(CreatedAtDisplay), ColumnName = "CreatedAt", SortOnWebServer = false, Title = "Skapat")]
        public string CreatedAtDisplay => CreatedAt.ToSwedishString("yyyy-MM-dd HH:mm");

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsLatest);
    }
}
