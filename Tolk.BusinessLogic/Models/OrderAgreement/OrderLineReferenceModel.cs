using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    public class OrderLineReferenceModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel LineID { get; set; }
    }
}
