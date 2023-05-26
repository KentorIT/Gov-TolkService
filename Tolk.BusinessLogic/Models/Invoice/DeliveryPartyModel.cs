using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class DeliveryPartyModel
    {
        [XmlElement(Namespace = Constants.cac)]
        public PartyNameModel PartyName = new PartyNameModel();
    }
}
