using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class AssignmentListModel
    {
        public AssignmentFilterModel FilterModel { get; set; }

        public IEnumerable<RequestListItemModel> Items { get; set; }
    }
}
