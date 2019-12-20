using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequisitionModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string TaxCard { get; set; }

        [Required]
        public DateTimeOffset AcctualStartedAt { get; set; }

        [Required]
        public DateTimeOffset AcctualEndedAt { get; set; }

        public int? WasteTime { get; set; }

        public int? WasteTimeInconvenientHour { get; set; }

        public IEnumerable<MealBreakModel> MealBreaks { get; set; }

        public decimal? Outlay { get; set; }

        public int? CarCompensation { get; set; }

        public string PerDiem { get; set; }

        [Required]
        public string Message { get; set; }
    }
}
