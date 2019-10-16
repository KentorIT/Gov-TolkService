using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{

    public class DynamicUserListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(Id), Visible = false)]
        public int Id { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(LastName), ColumnName = "NameFamily", SortOnWebServer = false, Title = "Efternamn")]
        public string LastName { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(FirstName), ColumnName = "NameFirst", SortOnWebServer = false, Title = "Förnamn")]
        public string FirstName { get; set; }

        [ColumnDefinitions(Index = 3, Name = nameof(Email), Title = "Epost")]
        public string Email { get; set; }

        public bool IsActive { get; set; }

        public string IsActiveDisplay => IsActive ? "Aktiv" : "Inaktiv";

        public string CombinedId { get; set; }

        public string IsLocalAdmin { get; set; }

        [ColumnDefinitions(Visible = false, Name = nameof(ColorClassName), IsLeftCssClassName = true)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);

        public static IQueryable<AspNetUser> Filter(CustomerUserFilterModel filters, IQueryable<AspNetUser> data)
        {
            var filteredData = data;
            if (!string.IsNullOrWhiteSpace(filters.SearchString))
            {
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
                filteredData = filteredData.Where(u =>
                    u.NameFirst.Contains(filters.SearchString) ||
                    u.NameFamily.Contains(filters.SearchString) ||
                    u.Email.Contains(filters.SearchString));
#pragma warning restore CA1307 // 
            }
            if (filters.UserType.HasValue)
            {
                switch (filters.UserType)
                {
                    case UserTypes.OrganisationAdministrator:
                        filteredData = filteredData.Where(u => u.Roles.Any(r => r.RoleId == filters.CentralAdministratorRoleId));
                        break;
                    case UserTypes.CentralOrderHandler:
                        filteredData = filteredData.Where(u => u.Roles.Any(r => r.RoleId == filters.CentralOrderHandlerRoleId));
                        break;
                    case UserTypes.OrderCreator:
                    default:
                        break;
                }
            }

            return filteredData;
        }
    }
}
