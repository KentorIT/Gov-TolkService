using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class OrganizationPartyModel
    {
        [XmlElement(Namespace = Constants.cac)]
        public PartyModel Party { get; set; }
    }
}
