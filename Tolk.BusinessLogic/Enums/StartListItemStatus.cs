using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum StartListItemStatus
    {

        [Description("Bokningsförfrågan avbokad")]
        OrderCancelled = 1,

        [Description("Bokningsförfrågan avböjd")]
        OrderNotAnswered = 2,

        [Description("Reklamationshändelse")]
        ComplaintEvent = 3,

        [Description("Rekvisition inkommen")]
        RequisitonArrived  = 4,

        [Description("Tolk tillsatt (godkännade krävs)")]
        OrderAcceptedForApproval = 5,

        [Description("Bokning skickad")]
        OrderCreated = 6,

        [Description("Tillsättning godkänd")]
        OrderApproved = 7,

        [Description("Inväntar rekvisition")]
        RequisitionAwaited = 8,

        [Description("Förfrågan inkommen")]
        RequestArrived = 9,

        [Description("Förfrågan mottagen")]
        RequestReceived = 10,

        [Description("Uppdrag utfört")]
        OrderDelivered = 11,

        [Description("Rekvistion underkänd")]
        RequisitionDenied = 12,

        [Description("Rekvistion skickad")]
        RequisitionCreated = 13,

    }
}
