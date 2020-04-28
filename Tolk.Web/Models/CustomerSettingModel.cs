using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class CustomerSettingModel
    {
        public CustomerSettingType CustomerSettingType { get; set; }
        public bool Value { get; set; }
    }
}
