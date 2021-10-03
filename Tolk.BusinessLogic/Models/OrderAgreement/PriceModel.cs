using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class PriceModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel PriceAmount { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public string PriceType
        {
            get => "CON";
            set { }
        }
    }
}
