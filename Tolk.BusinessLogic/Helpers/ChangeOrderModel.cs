using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Helpers
{
    public class ChangeOrderModel
    {
        public InterpreterLocation SelectedInterpreterLocation { get; set; }

        public string LocationStreet { get; set; }
        public string OffSiteContactInformation { get; set; }
        public string Description { get; set; }
        public string InvoiceReference { get; set; }
        public string CustomerReferenceNumber { get; set; }
        public string CustomerDepartment { get; set; }
        public OrderChangeLogType OrderChangeLogType { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int UpdatedBy { get; set; }
        public int? ImpersonatedUpdatedBy { get; set; }
        public IEnumerable<int> Attachments { get; set; }
    }
}
