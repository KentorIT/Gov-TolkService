using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class CustomerOrderAgreementSettingsViewModel : IModel
    {
        public int CustomerOrganisationId { get; set; }
        public string CustomerOrganisation { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public List<CustomerOrderAgreementBrokerListModel> CustomerOrderAgreementBrokerSettings { get; set; }

    }
}
