using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class StandardBusinessDocumentHeaderModel
    {
        [XmlElement(Namespace = Constants.sh)]
        public string HeaderVersion
        {
            get => "1.0";
            set { }
        }
        [XmlElement(Namespace = Constants.sh)]
        public PartnerModel Sender { get; set; }
        [XmlElement(Namespace = Constants.sh)]
        public PartnerModel Receiver { get; set; }
        [XmlElement(Namespace = Constants.sh)]
        public DocumentIdentificationModel DocumentIdentification { get; set; }
        [XmlElement(Namespace = Constants.sh)]
        public BusinessScopeModel BusinessScope { get; set; }
    }
}