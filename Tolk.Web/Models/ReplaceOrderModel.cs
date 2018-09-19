using System;
using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ReplaceOrderModel : OrderModel
    {
        [Display(Name = "Det ersatta avropets datum och tid")]
        public TimeRange ReplacedTimeRange { get; set; }

        [Display(Name = "Datum och tid för ersättning", Description = "Datum och tid för tolkuppdraget.")]
        [StayWithinOriginalRange(ErrorMessage = "Updraget måste ske inom tiden för det ersatta uppdraget", OtherRangeProperty = nameof(ReplacedTimeRange))]
        public override TimeRange TimeRange { get; set; }

        // Override original time range restrictions
        public override TimeRange AllowedTimeRange { get; set; }
    }
}
