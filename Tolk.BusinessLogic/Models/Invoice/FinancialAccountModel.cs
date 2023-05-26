using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Models.OrderAgreement;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class FinancialAccountModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel ID { get;set; }
        [XmlElement(Namespace = Constants.cac)]
        public ObjectWithIdModel FinancialInstitutionBranch { get; set; }
    }
}
