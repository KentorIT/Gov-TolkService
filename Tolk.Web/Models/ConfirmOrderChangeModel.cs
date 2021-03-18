using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ConfirmOrderChangeModel : IModel
    {
        public int RequestId { get; set; }

        public List<int> ConfirmedOrderChangeLogEntries { get; set; }

    }
}
