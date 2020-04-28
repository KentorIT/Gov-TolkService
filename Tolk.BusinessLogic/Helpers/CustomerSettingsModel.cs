using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Helpers
{
    [Serializable]
    public class CustomerSettingsModel
    {
        public int CustomerOrganisationId { get; set; }

        public IEnumerable<CustomerSettingType> UsedCustomerSettingTypes { get; set; }

    }
}
