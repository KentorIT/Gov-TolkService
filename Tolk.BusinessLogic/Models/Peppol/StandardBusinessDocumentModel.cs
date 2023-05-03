using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Models.OrderAgreement;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable] 
    [XmlRoot("StandardBusinessDocument",Namespace = Constants.sh)]    
    public class StandardBusinessDocumentModel
    {
        [XmlElement(Namespace = Constants.sh)]
        public StandardBusinessDocumentHeaderModel StandardBusinessDocumentHeader { get; set; }           
    }
}
