using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum NotificationType
    {
        [Description("Förfrågan skapad")]
        [CustomName("request_created")]
        RequestCreated = 1,

        [Description("x")]
        [CustomName("request_information_updated")]
        RequestInformationUpdated = 2,

        [Description("x")]
        [CustomName("request_cancelled_by_customer")]
        RequestCancelledByCustomer = 3,

        [Description("x")]
        [CustomName("request_cancelled_by_broker")]
        RequestCancelledByBroker= 4,

        [Description("x")]
        [CustomName("request_answer_approved")]
        RequestAnswerApproved = 5,

        [Description("x")]
        [CustomName("request_answer_denied")]
        RequestAnswerDenied= 6,

        [Description("x")]
        [CustomName("request_lost_due_to_inactivity")]
        RequestLostDueToInactivity = 7,

        [Description("x")]
        [CustomName("request_replacement_created")]
        RequestReplacementCreated = 8,

        [Description("x")]
        [CustomName("requisition_approved")]
        RequisitionApproved = 9,

        [Description("x")]
        [CustomName("requistion_denied")]
        RequisitionDenied = 10,

        [Description("x")]
        [CustomName("complaint_created")]
        ComplaintCreated = 11,

        [Description("x")]
        [CustomName("complaint_disputed_accepted")]
        ComplaintDisputedAccepted = 12,

        [Description("x")]
        [CustomName("complaint_dispute_pending_trial")]
        ComplaintDisputePendingTrial = 13,

        [Description("x")]
        [CustomName("complaint_terminated_trial_confirmed")]
        ComplaintTerminatedTrialConfirmed = 14,

        [Description("x")]
        [CustomName("complaint_terminated_trial_denied")]
        ComplaintTerminatedTrialDenied = 15,

        [Description("x")]
        [CustomName("request_replaced_interpreter_accepted")]
        RequestReplacedInterpreterAccepted = 16,
    }
}
