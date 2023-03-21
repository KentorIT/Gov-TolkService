using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class CustomerSpecificPropertyListItemModel
    {

        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(CompositeKeyRoute), Visible = false)]        
        public string CompositeKeyRoute => $"{CustomerOrganisationId}/{PropertyType}";
        [ColumnDefinitions(Index = 1, Name = nameof(PropertyTypeName), Title = "Ersatt Fält")]
        public string PropertyTypeName { get; set; }
        [ColumnDefinitions(Index = 2, Name = nameof(DisplayName), Title = "Nytt Fältnamn")]
        public string DisplayName { get; set; }
        [ColumnDefinitions(Index = 3, Name = nameof(RegexPattern), Title = "Regex")]
        public string RegexPattern { get; set; }
        [ColumnDefinitions(Index = 4, Name = nameof(PropertyType), Visible = false)]
        public PropertyType PropertyType { get; set; }
        [ColumnDefinitions(Index = 5, Name = nameof(CustomerOrganisationId), Visible = false)]
        public int CustomerOrganisationId { get; set; }
      
    }
}
