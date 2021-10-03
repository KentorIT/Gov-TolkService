using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class TaxSubtotalModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel TaxableAmount { get; set; }
        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel TaxAmount { get; set; }
        [XmlElement(Namespace = Constants.cac)]
        public TaxCategoryModel TaxCategory { get; set; }
    }
}
