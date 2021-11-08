using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class LegalMonetaryTotalModel
    {
        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel LineExtensionAmount { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel TaxExclusiveAmount
        {
            get => LineExtensionAmount;
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel TaxInclusiveAmount
        {
            get => new AmountModel
            {
                AmountSum = LineExtensionAmount.AmountSum + TaxSum
            };
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel PayableRoundingAmount
        {
            get => new AmountModel
            {
                AmountSum = GetRounding(TaxInclusiveAmount.AmountSum)
            };
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public AmountModel PayableAmount
        {
            get => new AmountModel
            {
                AmountSum = TaxInclusiveAmount.AmountSum + PayableRoundingAmount.AmountSum
            };
            set { }
        }

        [XmlIgnore]
        public decimal TaxSum { get; set; }

        private decimal GetRounding(decimal value)
        {
            value -= Math.Floor(value);
            return value > Convert.ToDecimal(0.5) ? 1 - value : -value;
        }
    }
}
