using System;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class StandardBusinessDocumentHeaderModel
    {
        public PartnerModel Sender { get; set; }
        public PartnerModel Reciever { get; set; }
        public string HeaderVersion
        {
            get => "1.0";
            set { }
        }
        public DocumentIdentificationModel DocumentIdentification { get; set; }
        public BusinessScopeModel BusinessScope { get; set; }
    }
}