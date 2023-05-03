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
        // This is Not used in SFTI:S Tekniska Kuvert but is used in the global standard, which one should be used?
        //[XmlElement(Namespace = Constants.sh)]
        //public BusinessScopeModel BusinessScope { get; set; }
    }
}