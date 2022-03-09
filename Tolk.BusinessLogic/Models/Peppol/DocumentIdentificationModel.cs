using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class DocumentIdentificationModel
    {
        public string Standard { get; set; }
        public string TypeVersion
        {
            get => "2.1";
            set { }
        }
        public string InstanceIdentifier { get; set; }
        public string Type { get; set; }
        [XmlIgnore]
        public DateTimeOffset CreatedAt { get; set; }
        public string CreationDateAndTime
        {
            get => CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            set { }
        }
    }
}