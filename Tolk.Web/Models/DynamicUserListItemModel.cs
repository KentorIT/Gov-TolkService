using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{

    public class DynamicUserListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(Id), Visible = false)]
        public int Id { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(LastName),  Title = "Efternamn")]
        public string LastName { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(FirstName), Title = "Förnamn")]
        public string FirstName { get; set; }

        [ColumnDefinitions(Index = 3, Name = nameof(Email), Title = "Epost")]
        public string Email { get; set; }

        public bool IsActive { get; set; }

        public string IsActiveDisplay => IsActive ? "Aktiv" : "Inaktiv";

        public string CombinedId { get; set; }

        public string IsLocalAdmin { get; set; }

        [ColumnDefinitions(Visible = false, Name = nameof(ColorClassName), IsLeftCssClassName = true)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);

        public IEnumerable<int> Roles { get; set; }

        public static IQueryable<DynamicUserListItemModel> Filter(CustomerUserFilterModel filters, IQueryable<DynamicUserListItemModel> data)
        {
            var filteredData = data;
            if (!string.IsNullOrWhiteSpace(filters.SearchString))
            {
                filteredData = filteredData.Where(u =>
                    u.FirstName.Contains(filters.SearchString) ||
                    u.LastName.Contains(filters.SearchString) ||
                    u.Email.Contains(filters.SearchString));
            }
            if (filters.UserType.HasValue)
            {
                switch (filters.UserType)
                {
                    case UserType.OrganisationAdministrator:
                        filteredData = filteredData.Where(u => u.Roles.Contains(filters.CentralAdministratorRoleId));
                        break;
                    case UserType.CentralOrderHandler:
                        filteredData = filteredData.Where(u => u.Roles.Contains(filters.CentralOrderHandlerRoleId));
                        break;
                    case UserType.OrderCreator:
                    default:
                        break;
                }
            }

            return filteredData;
        }
    }
}
