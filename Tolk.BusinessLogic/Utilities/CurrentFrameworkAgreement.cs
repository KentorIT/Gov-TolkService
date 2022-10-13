using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    [Serializable]
    public class CurrentFrameworkAgreement
    {
        public string AgreementNumber { get; set; }

        public string Description { get; set; }

        public DateTime FirstValidDate { get; set; }

        public DateTime LastValidDate { get; set; }

        public BrokerFeeCalculationType BrokerFeeCalculationType { get; set; }

        public FrameworkAgreementResponseRuleset FrameworkAgreementResponseRuleset { get; set; }

        public bool IsActive { get; set; }
    }
}
