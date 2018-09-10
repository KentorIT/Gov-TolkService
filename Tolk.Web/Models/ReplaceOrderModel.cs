using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ReplaceOrderModel : OrderModel
    {
        public TimeRange ReplacedTimeRange { get; set; }

        [Display(Name = "Datum och tid för ersättning", Description = "Datum och tid för tolkuppdraget.")]
        [StayWithinOriginalRange(ErrorMessage = "Updraget måste ske inom tiden för det ersatta uppdraget", OtherRangeProperty = nameof(ReplacedTimeRange))]
        public override TimeRange TimeRange { get; set; }
    }
}
