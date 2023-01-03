using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    [Serializable]
    public class CurrentOrLatestFrameworkAgreement
    {
        public int FrameworkAgreementId { get; set; }
        
        public string AgreementNumber { get; set; }

        public string Description { get; set; }

        public DateTime FirstValidDate { get; set; }

        public DateTime LastValidDate { get; set; }

        public DateTime OriginalLastValidDate { get; set; }

        public int PossibleAgreementExtensionsInMonths { get; set; }

        public BrokerFeeCalculationType BrokerFeeCalculationType { get; set; }

        public FrameworkAgreementResponseRuleset FrameworkAgreementResponseRuleset { get; set; }

        public bool IsActive { get; set; }
        
        public bool IsCurrentAndActiveFrameworkAgreement(int? frameworkAgreementId)
             => IsActive && FrameworkAgreementId == frameworkAgreementId;
    }
}
