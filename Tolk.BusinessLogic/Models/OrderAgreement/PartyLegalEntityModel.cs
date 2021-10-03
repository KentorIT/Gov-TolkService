using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class PartyLegalEntityModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string RegistrationName { get; set; }
    }
}
