using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class EndPointIDModel
    {
        [XmlAttribute("schemeID")]
        public string SchemeId { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
