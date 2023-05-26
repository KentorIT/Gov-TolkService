using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class CountryModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string IdentificationCode = "SE";
    }
}
