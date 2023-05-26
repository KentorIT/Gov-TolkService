using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Models.OrderAgreement;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class InvoiceLineModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel ID { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public QuantityModel InvoicedQuantity
        {
            get => new QuantityModel { Value = "1" };
            set { }
        }
        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel LineExtensionAmount { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public InvoicePeriodModel InvoicePeriod { get; set; }  

        [XmlElement(Namespace = Constants.cac)]
        public OrderLineReferenceModel OrderLineReference { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public ItemModel Item { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PriceModel Price { get; set; }
    }
}
