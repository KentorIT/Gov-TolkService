using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class TaxCategoryModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel ID
        {
            get => new EndPointIDModel { Value = "S" };
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public double Percent { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public ObjectWithIdModel TaxScheme
        {
            get => new ObjectWithIdModel { ID = new EndPointIDModel { Value = "VAT" } };
            set { }
        }
    }
}
