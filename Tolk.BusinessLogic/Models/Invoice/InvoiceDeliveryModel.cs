using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class InvoiceDeliveryModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public string ActualDeliveryDate { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public DeliveryPartyModel DeliveryParty { get; set; } = new DeliveryPartyModel();

    }
}

