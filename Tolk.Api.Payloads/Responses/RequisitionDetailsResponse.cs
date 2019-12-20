using System.Collections.Generic;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.WebHookPayloads;

namespace Tolk.Api.Payloads.Responses
{
    public class RequisitionDetailsResponse : ResponseBase
    {
        public string OrderNumber { get; set; }

        public string Status { get; set; }

        public string TaxCard { get; set; }

        public int? WasteTime { get; set; }

        public int? WasteTimeInconvenientHour { get; set; }

        public IEnumerable<MealBreakModel> MealBreaks { get; set; }

        public decimal? Outlay { get; set; }

        public int? CarCompensation { get; set; }

        public string PerDiem { get; set; }

        public string Message { get; set; }

        public PriceInformationModel PriceInformation { get; set; }

        public IEnumerable<RequisitionDetailsResponse> PreviousRequisitions { get; set; }

    }
}
