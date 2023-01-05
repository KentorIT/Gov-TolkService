using System;

namespace Tolk.Web.Models
{
    public class OrderSentModel
    {
        public string OrderNumber { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }
    }

}
