using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class PartyIdentificationModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel ID { get; set; }
    }
}
