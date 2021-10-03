using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class TaxTotalModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel TaxAmount { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public List<TaxSubtotalModel> TaxSubtotal { get; set; }
    }
}
