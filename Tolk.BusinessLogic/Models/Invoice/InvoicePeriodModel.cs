using System;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    public class InvoicePeriodModel
    {
        [XmlIgnore]
        public DateTimeOffset StartAt { get; set; }
        [XmlIgnore]
        public DateTimeOffset EndAt { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public string StartDate
        {
            get => StartAt.ToString("yyyy-MM-dd");
            set => StartAt = StartAt.AddDate(value);
        }
       
        [XmlElement(Namespace = Constants.cbc)]
        public string EndDate
        {
            get => EndAt.ToString("yyyy-MM-dd");
            set => EndAt = EndAt.AddDate(value);
        }
    }
}
