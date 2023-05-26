using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class PartyNameModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string Name { get; set; } = "Invoice Receiver";
    }
}
