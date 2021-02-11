using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum NotificationType
    {
        [Description("Förfrågan skapad")]
        [CustomName("request_created")]
        RequestCreated = 1,

        [Description("Uppdaterad information på förfrågan")]
        [CustomName("request_information_updated")]
        RequestInformationUpdated = 2,

        [Description("Uppdrag avbokat av myndighet")]
        [CustomName("request_cancelled_by_customer")]
        RequestCancelledByCustomer = 3,

        [Description("Sammanhållen förfrågan avbokad av myndighet")]
        [CustomName("request_group_cancelled_by_customer")]
        RequestGroupCancelledByCustomer = 4,

        [Description("Svar på förfrågan har accepterats")]
        [CustomName("request_answer_approved")]
        RequestAnswerApproved = 5,

        [Description("Svar på förfrågan godtogs inte")]
        [CustomName("request_answer_denied")]
        RequestAnswerDenied = 6,

        [Description("Förlorad förfrågan på grund av inaktivitet")]
        [CustomName("request_lost_due_to_inactivity")]
        RequestLostDueToInactivity = 7,

        [Description("Ersättningsuppdrag har inkommit")]
        [CustomName("request_replacement_created")]
        RequestReplacementCreated = 8,

        [Description("Rekvisition har granskats")]
        [CustomName("requisition_reviewed")]
        RequisitionReviewed = 9,

        [Description("Rekvisition har kommenterats")]
        [CustomName("requistion_commented")]
        RequisitionCommented = 10,

        [Description("Reklamation har skapats")]
        [CustomName("complaint_created")]
        ComplaintCreated = 11,

        [Description("Reklamation har återtagits av myndighet")]
        [CustomName("complaint_disputed_accepted")]
        ComplaintDisputedAccepted = 12,

        [Description("Reklamation har gått till extern process")]
        [CustomName("complaint_dispute_pending_trial")]
        ComplaintDisputePendingTrial = 13,

        [Description("Reklamation har blivit bistådd av extern process")]
        [CustomName("complaint_terminated_trial_confirmed")]
        ComplaintTerminatedTrialConfirmed = 14,

        [Description("Reklamation har blivit avslagen av extern process")]
        [CustomName("complaint_terminated_trial_denied")]
        ComplaintTerminatedTrialDenied = 15,

        [Description("Ersättning av tolk har godkänts")]
        [CustomName("request_replaced_interpreter_accepted")]
        RequestReplacedInterpreterAccepted = 16,

        [Description("Sammanhållen förfrågan skapad")]
        [CustomName("request_group_created")]
        RequestGroupCreated = 17,

        [Description("Förlorad samanhållen förfrågan på grund av inaktivitet")]
        [CustomName("request_group_lost_due_to_inactivity")]
        RequestGroupLostDueToInactivity = 18,

        [Description("Webhookanrop har fallerat för många gånger i rad")]
        [CustomName("error_notification")]
        ErrorNotification = 19,

        [Description("En ny myndighet har lagts upp i systemet")]
        [CustomName("customer_added")]
        CustomerAdded = 20,

        [Description("Svar på sammanhållen förfrågan har accepterats")]
        [CustomName("request_group_answer_approved")]
        RequestGroupAnswerApproved = 21,

        [Description("Svar på sammanhållen förfrågan godtogs inte")]
        [CustomName("request_group_answer_denied")]
        RequestGroupAnswerDenied = 22,

        [Description("Förlorad förfrågan på grund av ej besvarad av myndighet")]
        [CustomName("request_lost_due_to_no_answer_from_customer")]
        RequestLostDueToNoAnswerFromCustomer = 23,

        [Description("Förlorad samanhållen förfrågan på grund av ej besvarad av myndighet")]
        [CustomName("request_group_lost_due_to_no_answer_from_customer")]
        RequestGroupLostDueToNoAnswerFromCustomer = 24,

        [Description("Tid för tolkuppdrag passerad")]
        [CustomName("request_assignment_time_passed")]
        RequestAssignmentTimePassed = 25,

        [Description("Beställning fullständigt besvarad")]
        [CustomName("order_accepted", false)]
        OrderAccepted = 26,

        [Description("Beställning besvarad, inväntar godkännande av beställare")]
        [CustomName("order_answered", false)]
        OrderAnswered = 27,

        [Description("Beställning besvarad, inväntar godkännande av beställare")]
        [CustomName("order_declined", false)]
        OrderDeclined = 28,
    }
}