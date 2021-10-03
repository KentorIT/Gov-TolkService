using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class LineItemModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel ID { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public string Note { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public QuantityModel Quantity
        {
            get => new QuantityModel { Value = "1" };
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel LineExtensionAmount
        {
            get => new AmountModel { AmountSum = Price.PriceAmount.AmountSum };
            set { }
        }

        [XmlElement(Namespace = Constants.cac)]
        public DeliveryModel Delivery { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PriceModel Price { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public ItemModel Item { get; set; }
    }
}
