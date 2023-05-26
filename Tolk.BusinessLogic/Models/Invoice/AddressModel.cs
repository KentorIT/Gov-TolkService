using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class AddressModel
    {
        [XmlElement(Namespace = Constants.cac)]
        public CountryModel Country = new CountryModel();
    }
}
