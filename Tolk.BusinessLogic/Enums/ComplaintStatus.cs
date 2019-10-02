using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum ComplaintStatus
    {
        [CustomName("complaint_created")]
        [Description("Reklamation är skapad av myndighet")]
        Created = 1,
        [CustomName("complaint_confirmed")]
        [Description("Reklamation är godtagen av förmedling")]
        Confirmed = 2,
        [CustomName("complaint_disputed")]
        [Description("Reklamation är bestriden av förmedling")]
        Disputed = 3,
        [CustomName("complaint_terminated_as_dispute_accepted")]
        [Description("Reklamation är återtagen av myndighet")]
        TerminatedAsDisputeAccepted = 4,
        [CustomName("complaint_dispute_pending_trial")]
        [Description("Reklamation kvarstår")]
        DisputePendingTrial = 5,
        [CustomName("complaint_terminated_trial_confirmed_complaint")]
        [Description("Reklamation bistådd av extern process")]
        TerminatedTrialConfirmedComplaint = 6,
        [CustomName("complaint_terminated_trial_denied_complaint")]
        [Description("Reklamation avslagen av extern process")]
        TerminatedTrialDeniedComplaint = 7,
        [CustomName("complaint_automatically_confirmed")]
        [Description("Reklamation automatiskt godtagen då svar uteblivit")]
        AutomaticallyConfirmedDueToNoAnswer = 8,
    }
}
