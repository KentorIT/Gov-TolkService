using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class PeppolMessageListModel
    {
        public PeppolMessageFilterModel FilterModel { get; set; }
        public UserPageMode UserPageMode { get; set; }
        public bool IsApplicationAdmin { get; set; }


        [NoDisplayName]
        [Placeholder("BokningsID att skapa order agreement/order response för")]
        public string OrderNumber { get; set; }

    }
}
