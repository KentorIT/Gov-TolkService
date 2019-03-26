using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum ReportType
    {
        [Description("Bokningsförfrågningar")]
        Orders = 1,
        [Description("Utförda tolkuppdrag")]
        DeliveredOrders = 2,
        //[Description("Rekvisitioner")]
        //Requisitions = 3,
        //[Description("Reklamationer")]
        //Complaints = 4,
    }
}
