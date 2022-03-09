using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Models.OrderAgreement;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    [XmlRoot("StandardBusinessDocument")]
    public class StandardBusinessDocumentModel
    {
        public StandardBusinessDocumentHeaderModel StandardBusinessDocumentHeader { get; set; }
        public OrderAgreementModel OrderResponse { get; set; }
    }
}
