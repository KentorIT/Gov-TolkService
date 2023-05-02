using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class PartnerIdentifier
    {
        [XmlAttribute]
        public string Authority
        {
            get => "iso6523-actorid-upis";
            set { }
        }
        [XmlText]
        public string Identifier { get; set; }
    }
}
