using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum NotificationType
    {

        [Description("Okänd notifieringstyp")]
        [CustomName("unknown_type", false)]
        UnkonwnType = -1,

        [Description("Förfrågan skapad")]
        [CustomName("request_created")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestCreated = 1,

        [Description("Uppdaterad information på förfrågan")]
        [CustomName("request_information_updated")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestInformationUpdated = 2,

        [Description("Uppdrag avbokat av myndighet")]
        [CustomName("request_cancelled_by_customer")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestCancelledByCustomer = 3,

        [Description("Sammanhållen förfrågan avbokad av myndighet")]
        [CustomName("request_group_cancelled_by_customer")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestGroupCancelledByCustomer = 4,

        [Description("Svar på förfrågan har accepterats")]
        [CustomName("request_answer_approved")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestAnswerApproved = 5,

        [Description("Svar på förfrågan godtogs inte")]
        [CustomName("request_answer_denied")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestAnswerDenied = 6,

        [Description("Förlorad förfrågan på grund av inaktivitet")]
        [CustomName("request_lost_due_to_inactivity")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestLostDueToInactivity = 7,

        [Description("Ersättningsuppdrag har inkommit")]
        [CustomName("request_replacement_created")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestReplacementCreated = 8,

        [Description("Rekvisition har granskats")]
        [CustomName("requisition_reviewed")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequisitionReviewed = 9,

        [Description("Rekvisition har kommenterats")]
        [CustomName("requistion_commented")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequisitionCommented = 10,

        [Description("Reklamation har skapats")]
        [CustomName("complaint_created")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        ComplaintCreated = 11,

        [Description("Reklamation har återtagits av myndighet")]
        [CustomName("complaint_disputed_accepted")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        ComplaintDisputedAccepted = 12,

        [Description("Reklamation har gått till extern process")]
        [CustomName("complaint_dispute_pending_trial")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        ComplaintDisputePendingTrial = 13,

        [Description("Reklamation har blivit bistådd av extern process")]
        [CustomName("complaint_terminated_trial_confirmed")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        ComplaintTerminatedTrialConfirmed = 14,

        [Description("Reklamation har blivit avslagen av extern process")]
        [CustomName("complaint_terminated_trial_denied")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        ComplaintTerminatedTrialDenied = 15,

        [Description("Ersättning av tolk har godkänts")]
        [CustomName("request_replaced_interpreter_accepted")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequestReplacedInterpreterAccepted = 16,

        [Description("Sammanhållen förfrågan skapad")]
        [CustomName("request_group_created")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestGroupCreated = 17,

        [Description("Förlorad samanhållen förfrågan på grund av inaktivitet")]
        [CustomName("request_group_lost_due_to_inactivity")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestGroupLostDueToInactivity = 18,

        [Description("Webhookanrop har fallerat för många gånger i rad")]
        [CustomName("error_notification")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        //NotificationConsumerType Broker, Customer, second line support
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        [NotificationConsumerType(NotificationConsumerType.SecondLineSupport)]
        ErrorNotification = 19,

        [Description("En ny myndighet har lagts upp i systemet")]
        [CustomName("customer_added")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        CustomerAdded = 20,

        [Description("Svar på sammanhållen förfrågan har accepterats")]
        [CustomName("request_group_answer_approved")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestGroupAnswerApproved = 21,

        [Description("Svar på sammanhållen förfrågan godtogs inte")]
        [CustomName("request_group_answer_denied")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestGroupAnswerDenied = 22,

        [Description("Förlorad förfrågan på grund av ej besvarad av myndighet")]
        [CustomName("request_lost_due_to_no_answer_from_customer")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestLostDueToNoAnswerFromCustomer = 23,

        [Description("Förlorad samanhållen förfrågan på grund av ej besvarad av myndighet")]
        [CustomName("request_group_lost_due_to_no_answer_from_customer")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestGroupLostDueToNoAnswerFromCustomer = 24,

        [Description("Tid för tolkuppdrag passerad")]
        [CustomName("request_assignment_time_passed")]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        RequestAssignmentTimePassed = 25,

        [Description("Beställning fullständigt besvarad")]
        [CustomName("order_accepted", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderAccepted = 26,

        [Description("Beställning besvarad, inväntar godkännande av beställare")]
        [CustomName("order_answered", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderAnswered = 27,

        [Description("Beställning avböjd av förmedling")]
        [CustomName("order_declined", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderDeclined = 28,

        [Description("Beställning avbokad av förmedling")]
        [CustomName("order_cancelled_by_broker", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [AvailableNotificationChannel(NotificationChannel.Webhook)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderCancelledByBroker = 29,

        [Description("Order agreement skapat i systemet")]
        [CustomName("order_agreement_created", false)]
        [AvailableNotificationChannel(NotificationChannel.Peppol)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderAgreementCreated = 30,

        [Description("Tidigare skapad order agreement ersatt i systemet")]
        [CustomName("order_agreement_replaced", false)]
        [AvailableNotificationChannel(NotificationChannel.Peppol)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderAgreementRepalced = 31,





        //Additional
        [Description("Uppdrag avbokat av myndighet efter godkänd bekräftelse")]
        [CustomName("request_cancelled_by_customer_when_approved", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Broker)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequestCancelledByCustomerWhenApproved = 32,

        [Description("Behörighet att granska rekvisition tillagd")]
        [CustomName("requisition_approval_rights_added", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequisitionApprovalRightsAdded = 33,

        [Description("Behörighet att granska rekvisition borttagen")]
        [CustomName("requisition_approval_rights_removed", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequisitionApprovalRightsRemoved = 34,

        [Description("Bokningsförfrågan avslutad")]
        [CustomName("order_terminated", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderTerminated = 35,

        [Description("Bokningsförfrågan avslutad")]
        [CustomName("order_group_terminated", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderGroupTerminated = 36,

        [Description("Bokningsförfrågan skapad utan sista svarstid")]
        [CustomName("request_created_without_expiry", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequestCreatedWithoutExpiry = 37,

        [Description("Sammanhållen bokningsförfrågan skapad utan sista svarstid")]
        [CustomName("request_group_created_without_expiry", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequestgroupCreatedWithoutExpiry = 38,

        [Description("Sammanhållen bokningsförfrågan fullständigt besvarad")]
        [CustomName("order_group_accepted", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        //NotificationConsumerType Customer
        OrderGroupAccepted = 39,

        [Description("Reklamation har godtagits")]
        [CustomName("complaint_confirmed", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        ComplaintConfirmed = 40,

        [Description("Reklamation har bestridits")]
        [CustomName("complaint_disputed", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        ComplaintDisputed = 41,

        [Description("Rekvisition har skapats")]
        [CustomName("requisition_created", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RequisitionCreated = 42,

        [Description("Sammanhållen bokningsförfrågan avböjd av förmedling")]
        [CustomName("order_group_declined", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        OrderGroupDeclined = 43,

        [Description("Ersättningsuppdrag fullständigt besvarad")]
        [CustomName("replacement_order_accepted", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        ReplamentOrderAccepted = 44,

        [Description("Ersättningsuppdrag accepterat och automatiskt godkänt")]
        [CustomName("replacement_order_approved", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        ReplamentOrderApproved = 45,

        [Description("Ersättningsuppdrag avböjt")]
        [CustomName("replacement_order_declined", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        ReplamentOrderDeclined = 46,

        [Description("Tolk ersatt på uppdrag")]
        [CustomName("interpreter_changed", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        InterpreterChanged = 47,

        [Description("Svar på förfrågan väntar på hantering")]
        [CustomName("remind_unhandled_request", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RemindUnhandledRequest = 48,

        [Description("Svar på sammanhållen bokningsförfrågan väntar på hantering")]
        [CustomName("remind_unhandled_request_group", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        RemindUnhandledRequestGroup = 49,

        [Description("Del av sammanhållen bokningsförfrågan besvarad")]
        [CustomName("partial_request_group_accepted", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        PartialRequestGroupAccepted = 50,

        [Description("Del av sammanhållen bokningsförfrågan besvarad")]
        [CustomName("partial_request_group_automatically_approved", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.Customer)]
        PartialRequestGroupAutomaticallyApproved = 51,

        [Description("Inbjudan för användande av systemet")]
        [CustomName("user_invitation", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.User)]
        UserInvitation = 52,

        [Description("Begäran om verifikation av ändrad epost")]
        [CustomName("changed_email_verification", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.User)]
        ChangedEmailVerification = 53,

        [Description("Varning om många användare skapade med samma prefix")]
        [CustomName("generated_user_prefix_limit_warning", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.SecondLineSupport)]
        GeneraratedUserPrefixLimitWarning = 54,

        [Description("Varning om misslyckad språkuppdatering från Tellus")]
        [CustomName("get_languages_from_tellus_warning", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.SecondLineSupport)]
        GetLaguagesFromTellusFailed = 55,

        [Description("Varning om misslyckad kompetensuppdatering från Tellus")]
        [CustomName("get_competences_from_tellus_warning", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.SecondLineSupport)]
        GetCompetencesFromTellusFailed = 55,

        [Description("Återställande för bortglömt lösenord")]
        [CustomName("password_reset", false)]
        [AvailableNotificationChannel(NotificationChannel.Email)]
        [NotificationConsumerType(NotificationConsumerType.User)]
        PasswordReset = 56,
    }
}