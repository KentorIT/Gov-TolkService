using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderAgreementListModel
    {
        public OrderAgreementFilterModel FilterModel { get; set; }
        public UserPageMode UserPageMode { get; set; }
        public bool IsApplicationAdmin { get; set; }


        [NoDisplayName]
        [Placeholder("BokningsID att skapa order agreement för")]
        public string OrderNumber { get; set; }

    }
}
