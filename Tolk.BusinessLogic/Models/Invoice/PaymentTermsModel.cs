using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class PaymentTermsModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string Note = "30 dagar netto";
    }
}
