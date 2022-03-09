using System;

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

        public static ScopeModel DocumentScope =>
            new ScopeModel(Constants.CustomizationId, "DOCUMENTID", "busdox-docid-qns");
        public static ScopeModel ProcessScope =>
            new ScopeModel(Constants.ProfileId, "PROCESSID", "cenbii-procid-ubl");
    }
}