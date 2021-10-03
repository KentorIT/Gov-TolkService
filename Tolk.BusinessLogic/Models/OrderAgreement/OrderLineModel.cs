using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class OrderLineModel
    {
        [XmlElement(Namespace = Constants.cac)]
        public LineItemModel LineItem { get; set; }
    }
}
