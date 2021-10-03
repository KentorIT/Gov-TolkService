using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class PartyModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel EndpointID { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PartyIdentificationModel PartyIdentification { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PartyLegalEntityModel PartyLegalEntity { get; set; }
    }
}
