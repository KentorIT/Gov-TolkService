using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class DeliveryModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public QuantityModel Quantity
        {
            get => new QuantityModel { Value = "1" };
            set { }
        }
        [XmlElement(Namespace = Constants.cac)]
        public PromisedDeliveryPeriodModel PromisedDeliveryPeriod { get; set; }
    }
}
