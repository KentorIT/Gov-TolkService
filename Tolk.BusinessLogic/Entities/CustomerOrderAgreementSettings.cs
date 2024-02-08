using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerOrderAgreementSettings
    {
        public int CustomerOrganisationId { get; set; }
        public int BrokerId { get; set; }
        public DateTimeOffset? EnabledAt { get; set; }        
        public bool Disabled => EnabledAt == null;        
        #region foreign keys
        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }
        [ForeignKey(nameof(BrokerId))]
        public Broker Broker{ get; set; }
        #endregion
    }
}
