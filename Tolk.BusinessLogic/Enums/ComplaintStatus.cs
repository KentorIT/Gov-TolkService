using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum ComplaintStatus
    {
        [Description("Reklamation är skapad av myndighet")]
        Created = 1,
        [Description("Reklamation är godtagen av förmedling")]
        Confirmed = 2,
        [Description("Reklamation är bestridd av förmedling")]
        Disputed = 3,
        [Description("Reklamation är återtagen av myndighet")]
        TerminatedAsDisputeAccepted = 4,
        [Description("Reklamation kvarstår")]
        DisputePendingTrial = 5,
        [Description("Reklamation bistådd av extern process")]
        TerminatedTrialConfirmedComplaint = 6,
        [Description("Reklamation avslagen av extern process")]
        TerminatedTrialDeniedComplaint = 7,
    }
}
