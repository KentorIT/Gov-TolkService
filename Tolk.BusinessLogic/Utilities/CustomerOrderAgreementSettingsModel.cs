using System;

namespace Tolk.BusinessLogic.Utilities
{
    public class CustomerOrderAgreementSettingsModel
    {
        public int CustomerOrganisationId { get; set; }
        public string CustomerName { get; set; }
        public int BrokerId { get; set; }
        public string BrokerName { get; set; }
        public DateTimeOffset? EnabledAt { get; set; }
        public bool Disabled => EnabledAt == null;        
    }
}
