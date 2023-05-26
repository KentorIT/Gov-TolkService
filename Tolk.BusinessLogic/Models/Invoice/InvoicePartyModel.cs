using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Models.OrderAgreement;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class InvoicePartyModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel EndpointID { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public AddressModel PostalAddress { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PartyTaxSchemeModel PartyTaxScheme { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PartyIdentificationModel PartyIdentification { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public PartyLegalEntityModel PartyLegalEntity { get; set; }
    }
}
