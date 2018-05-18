using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class StartViewModel
    {
        public string Message { get; set; }
        public IEnumerable<StartPageBox> Boxes { get; set; }
    }

    public class StartPageBox
    {
        public string Header { get; set; }
        public int Count { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public IDictionary<string, string> RouteData { get; set; }
    }
}
