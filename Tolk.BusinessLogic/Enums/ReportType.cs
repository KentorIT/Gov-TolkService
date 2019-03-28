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
        [CustomName("Uppdragsdatum")]
        [Description("Utförda tolkuppdrag")]
        DeliveredOrdersSystemAdministrator = 5,
        [CustomName("Beställningsdatum")]
        [Description("Bokningsförfrågningar")]
        OrdersForSystemAdministrator = 6,
        [CustomName("Rekvisitionsdatum")]
        [Description("Rekvisitioner")]
        RequisitionsForCustomer = 7,
        [CustomName("Rekvisitionsdatum")]
        [Description("Rekvisitioner")]
        RequisitionsForBroker = 8,
        [CustomName("Rekvisitionsdatum")]
        [Description("Rekvisitioner")]
        RequisitionsForSystemAdministrator = 9,
        [CustomName("Reklamationsdatum")]
        [Description("Reklamationer")]
        ComplaintsForCustomer = 10,
        [CustomName("Reklamationsdatum")]
        [Description("Reklamationer")]
        ComplaintsForBroker = 11,
        [CustomName("Reklamationsdatum")]
        [Description("Reklamationer")]
        ComplaintsForSystemAdministrator = 12,
    }
}
