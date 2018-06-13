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

        public IEnumerable<ConfirmationMessage> ConfirmationMessages { get; set; }

        public class StartPageBox
        {
            public string Header { get; set; }
            public int Count { get; set; }
            public string Controller { get; set; }
            public string Action { get; set; }
        }

        public class ConfirmationMessage
        {
            public string Header { get; set; }

            public string Message { get; set; }

            public string Controller { get; set; }

            public string Action { get; set; }

            public int Id { get; set; }
        }
    }
}
