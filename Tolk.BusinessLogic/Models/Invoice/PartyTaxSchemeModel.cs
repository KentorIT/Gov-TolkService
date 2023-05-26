using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Models.OrderAgreement;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class PartyTaxSchemeModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string CompanyID { get; set; }
        [XmlElement(Namespace = Constants.cac)]        
        public ObjectWithIdModel TaxScheme
        {
            get => new ObjectWithIdModel { ID = new EndPointIDModel { Value = "VAT" } };
            set { }
        }
    }
}
