using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]
    public class PromisedDeliveryPeriodModel
    {
        [XmlIgnore]
        public DateTimeOffset StartAt { get; set; }
        [XmlIgnore]
        public DateTimeOffset EndAt { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public string StartDate
        {
            get => StartAt.ToString("yyyy-MM-dd");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string StartTime
        {
            get => StartAt.ToString("HH:mm:ss");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string EndDate
        {
            get => EndAt.ToString("yyyy-MM-dd");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string EndTime
        {
            get => EndAt.ToString("HH:mm:ss");
            set { }
        }
    }
}
