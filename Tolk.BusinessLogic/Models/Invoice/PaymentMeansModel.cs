using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class PaymentMeansModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string PaymentMeansCode = Constants.CreditTransferPaymentMeansCode;
        [XmlElement(Namespace = Constants.cbc)]
        public string PaymentID = "Test-ID";
        [XmlElement(Namespace = Constants.cac)]
        public FinancialAccountModel PayeeFinancialAccount { get; set; }
    }
}
