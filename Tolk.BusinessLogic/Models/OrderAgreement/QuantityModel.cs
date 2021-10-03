using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class QuantityModel
    {
        [XmlAttribute("unitCode")]
        public string UnitCode
        {
            get => "EA";
            set { }
        }
        [XmlText]
        public string Value { get; set; }
    }
}
