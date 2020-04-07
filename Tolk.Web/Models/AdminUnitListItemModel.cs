using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class AdminUnitListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(CustomerUnitId), Visible = false)]
        public int CustomerUnitId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(Name), Title = "Namn")]
        public string Name { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(Email), Title = "E-postadress")]
        public string Email { get; set; }

        public bool IsActive { get; set; }

        [ColumnDefinitions(Visible = false, Name = nameof(ColorClassName), IsLeftCssClassName = true)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);

        internal static IQueryable<CustomerUnit> Filter(AdminUnitFilterModel filters, IQueryable<CustomerUnit> data)
        {
            var filteredData = data;
            if (!string.IsNullOrWhiteSpace(filters.SearchString))
            {
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
                filteredData = filteredData.Where(u =>
                    u.Name.Contains(filters.SearchString) ||
                    u.Email.Contains(filters.SearchString));
#pragma warning restore CA1307 // 
            }
            return filteredData;
        }
    }
}
