using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class CustomerOrderAgreementBrokerListModel : IModel
    {                        
        public int BrokerId { get; set; }        
        public string BrokerName { get; set; }
        public bool Disabled { get; set; }        
        public string DisabledDisplay => (!Disabled).ToSwedishString();        
        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(!Disabled);
    }
}