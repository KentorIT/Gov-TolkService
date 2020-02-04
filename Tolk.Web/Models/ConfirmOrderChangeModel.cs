using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ConfirmOrderChangeModel
    {
        public int RequestId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<int> ConfirmedOrderChangeLogEntries { get; set; }

    }
}
