using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum ReportType
    {
        //CustomName for this enum is used as the string to display as search criteria for reports
        [CustomName("Beställningsdatum")]
        [Description("Bokningsförfrågningar")]
        OrdersForCustomer = 1,
        [CustomName("Uppdragsdatum")]
        [Description("Utförda tolkuppdrag")]
        DeliveredOrdersCustomer = 2,
        [CustomName("Inkommendatum")]
        [Description("Bokningsförfrågningar")]
        RequestsForBrokers = 3,
        [CustomName("Uppdragsdatum")]
        [Description("Utförda tolkuppdrag")]
        DeliveredOrdersBrokers = 4,
    }
}
