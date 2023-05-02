using System;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class PartnerModel
    {
        private PartnerModel() { }

        public PartnerModel(string identifier)
        {
            Identifier = new PartnerIdentifier {Identifier = identifier } ;
        }   
        public PartnerIdentifier Identifier { get; set; }
    }
}