using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class ScopeModel
    {
        private ScopeModel() { }

        public ScopeModel(string instanceIdentifier, string type, string identifier)
        {
            InstanceIdentifier = instanceIdentifier;
            Type = type;
            Identifier = identifier;
        }
        public string InstanceIdentifier { get; set; }
        public string Type { get; set; }
        public string Identifier { get; set; }

        private static ScopeModel OrderAgreementDocumentScope =>
            new ScopeModel(Constants.OrderAgreementCustomizationId, "DOCUMENTID", "busdox-docid-qns");
        private static ScopeModel OrderAgreementProcessScope =>
            new ScopeModel(Constants.OrderAgreementProfileId, "PROCESSID", "cenbii-procid-ubl");

        private static ScopeModel OrderResponseDocumentScope =>
          new ScopeModel(Constants.OrderResponseCustomizationId, "DOCUMENTID", "busdox-docid-qns");
        private static ScopeModel OrderResponseProcessScope =>
            new ScopeModel(Constants.OrderResponseProfileId, "PROCESSID", "cenbii-procid-ubl");

        public static List<ScopeModel> GetScopeModelForType(PeppolMessageType type)
        {
            switch (type)
            {
                case PeppolMessageType.OrderAgreement:
                    return new List<ScopeModel>
                    {
                        OrderAgreementDocumentScope,
                        OrderAgreementProcessScope
                    };                    
                case PeppolMessageType.OrderResponse:
                    return new List<ScopeModel>
                    {
                        OrderResponseDocumentScope,
                        OrderResponseProcessScope
                    };                    
                default:
                    throw new InvalidOperationException($"PeppolMessageType: {type} is not a valid type.");                    
            }            
        }
    }
}