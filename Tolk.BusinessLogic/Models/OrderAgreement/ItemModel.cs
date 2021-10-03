using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class ItemModel
    {
        //Use the price calculation description here?
        [XmlElement(Namespace = Constants.cbc)]
        public string Description { get; set; }
        [XmlElement(Namespace = Constants.cbc)]
        public string Name { get; set; }
        [XmlElement(Namespace = Constants.cac)]
        public ObjectWithIdModel SellersItemIdentification { get; set; }
        [XmlElement(Namespace = Constants.cac)]
        public TaxCategoryModel ClassifiedTaxCategory { get; set; }
    }
}
