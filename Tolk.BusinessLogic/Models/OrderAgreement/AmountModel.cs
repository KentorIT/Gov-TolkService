using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class AmountModel
    {
        [XmlAttribute("currencyID")]
        public string CurrencyId
        {
            get => Constants.Currency;
            set { }
        }
        [XmlText]
        public string Value
        {
            get => AmountSum.ToEnglishString("#0.00");
            set { }
        }

        [XmlIgnore]
        public decimal AmountSum { get; set; }
    }
}
