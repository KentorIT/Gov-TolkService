using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class LanguageListItem
    {
        public string Name { get; set; }

        public string ISO_639_Code { get; set; }

        public string TellusName { get; set; }

        public bool HasLegal { get; set; }

        public bool HasHealthcare { get; set; }

        public bool HasAuthorized { get; set; }

        public bool HasEducated { get; set; }

    }
}
