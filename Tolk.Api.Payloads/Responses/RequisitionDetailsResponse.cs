using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.Responses
{
    public class RequisitionDetailsResponse : ResponseBase
    {
        public string OrderNumber { get; set; }

        public string Status { get; set; }

        public string TaxCard { get; set; }

        public string Message { get; set; }

        public IEnumerable<RequisitionDetailsResponse> PreviousRequisitions { get; set; }
    }
}
