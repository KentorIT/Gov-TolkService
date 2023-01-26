using System;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class DisplayContractModel
    {                
        public string AgreementNumber { get; set; }        
        public string Description { get; set; }        
        public DateTime FirstValidDate { get; set; }        
        public DateTime OriginalLastValidDate { get; set; }                
        public DateTime LastValidDate { get; set; }
        public bool IsActive { get; set; }
        public ContractDefinition ContractDefinition { get; set; }
        public FrameworkAgreementResponseRuleset FrameworkAgreementResponseRuleset { get; set; }
    }
}
