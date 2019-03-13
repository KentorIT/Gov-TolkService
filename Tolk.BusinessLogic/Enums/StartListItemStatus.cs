using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum StartListItemStatus
    {

        [Description("Bokningsförfrågan avbokad")]
        OrderCancelled = 1,

        [Description("Bokningsförfrågan ej besvarad")]
        OrderNotAnswered = 2,

        [Description("Reklamationshändelse")]
        ComplaintEvent = 3,

        [Description("Rekvisition inkommen")]
        RequisitonArrived  = 4,

        [Description("Tolk tillsatt (godkännande krävs)")]
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
        RequisitionToBeCreated = 11,

        [Description("Rekvisition har kommenterats av myndighet")]
        RequisitionCommented = 12,

        [Description("Rekvisition skickad")]
        RequisitionCreated = 13,

        [Description("Tillsättning avböjd")]
        RequestDenied = 14,

        [Description("Ersättningsuppdrag inkommet")]
        ReplacementOrderRequestArrived= 15,

        [Description("Ersättningsuppdrag mottaget")]
        ReplacementOrderRequestReceived = 16,

        [Description("Ersättningsuppdrag avböjt/ej besvarat")]
        ReplacementOrderNotAnswered = 17,

        [Description("Tolk är ersatt med ny tolk (godkännande krävs)")]
        NewInterpreterForApproval = 18,

        [Description("Sista svarstid ej satt")]
        AwaitingDeadlineFromCustomer = 19,

        [Description("Ersättningsuppdrag skickat")]
        ReplacementOrderCreated = 20,
    }
}
