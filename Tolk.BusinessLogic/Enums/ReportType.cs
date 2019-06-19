using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum ReportType
    {
        //CustomName for this enum is used as the string to display as search criteria for reports
        [CustomName("Beställningsdatum")]
        [Description("Bokningsförfrågningar")]
        [Parent(ReportGroup.CustomerReport)]
        OrdersForCustomer = 1,

        [CustomName("Uppdragsdatum")]
        [Description("Utförda tolkuppdrag")]
        [Parent(ReportGroup.CustomerReport)]
        DeliveredOrdersCustomer = 2,

        [CustomName("Inkommendatum")]
        [Description("Bokningsförfrågningar")]
        [Parent(ReportGroup.BrokerReport)]
        RequestsForBrokers = 3,

        [CustomName("Uppdragsdatum")]
        [Description("Utförda tolkuppdrag")]
        [Parent(ReportGroup.BrokerReport)]
        DeliveredOrdersBrokers = 4,

        [CustomName("Uppdragsdatum")]
        [Description("Utförda tolkuppdrag")]
        [Parent(ReportGroup.SystemAdminReport)]
        DeliveredOrdersSystemAdministrator = 5,

        [CustomName("Beställningsdatum")]
        [Description("Bokningsförfrågningar")]
        [Parent(ReportGroup.SystemAdminReport)]
        OrdersForSystemAdministrator = 6,

        [CustomName("Rekvisitionsdatum")]
        [Description("Rekvisitioner")]
        [Parent(ReportGroup.CustomerReport)]
        RequisitionsForCustomer = 7,

        [CustomName("Rekvisitionsdatum")]
        [Description("Rekvisitioner")]
        [Parent(ReportGroup.BrokerReport)]
        RequisitionsForBroker = 8,

        [CustomName("Rekvisitionsdatum")]
        [Description("Rekvisitioner")]
        [Parent(ReportGroup.SystemAdminReport)]
        RequisitionsForSystemAdministrator = 9,

        [CustomName("Reklamationsdatum")]
        [Description("Reklamationer")]
        [Parent(ReportGroup.CustomerReport)]
        ComplaintsForCustomer = 10,

        [CustomName("Reklamationsdatum")]
        [Description("Reklamationer")]
        [Parent(ReportGroup.BrokerReport)]
        ComplaintsForBroker = 11,

        [CustomName("Reklamationsdatum")]
        [Description("Reklamationer")]
        [Parent(ReportGroup.SystemAdminReport)]
        ComplaintsForSystemAdministrator = 12,
    }
}
