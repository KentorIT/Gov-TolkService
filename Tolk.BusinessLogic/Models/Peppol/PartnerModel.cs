using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class PartnerModel
    {
        private PartnerModel() { }

        public PartnerModel(string identifier)
        {
            Identifier = identifier;
        }
        public string Identifier { get; set; }

        [XmlAttribute]
        public string Authority
        {
            get => "iso6523-actorid-upis";
            set { }
        }
    }
}