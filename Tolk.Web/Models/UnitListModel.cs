using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class UnitListModel
    {
        public UnitFilterModel FilterModel { get; set; }

        public IEnumerable<UnitListItemModel> Items { get; set; }

        public bool AllowCreation { get; set; }
    }
}
