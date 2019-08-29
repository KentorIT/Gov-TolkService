using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    public class ColumnDefinition
    {
        public bool IsIdColumn { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }
        public string Title { get; set; }
        public bool Sortable { get; set; }
        public bool Visible { get; set; }
        public bool IsLeftCssClassName { get; set; }
        public bool Searchable => false;
    }
}
