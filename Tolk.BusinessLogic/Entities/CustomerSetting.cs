using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerSetting
    {
        public int CustomerOrganisationId { get; set; }

        public CustomerSettingType CustomerSettingType { get; set; }

        public bool Value { get; set; }

        #region foreign keys

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        #endregion
    }
}
