using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class InvoiceOrganizationPartyModel
    {
        
        [XmlElement(Namespace = Constants.cac)]
        public InvoicePartyModel Party { get; set; }
    }
}
