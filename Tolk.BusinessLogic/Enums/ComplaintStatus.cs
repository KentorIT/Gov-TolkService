using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum ComplaintStatus
    {
        [Description("Skapad")]
        Created = 1,
        [Description("Reklamation accepterad av förmedling")]
        Confirmed = 2,
        [Description("Reklamation bestridd av förmedling")]
        Disputed = 3,
        [Description("Bestridande accepterat")]
        TerminatedAsDisputeAccepted = 4,
        [Description("Avvaktar extern process")]
        DisputePendingTrial = 5,
        [Description("Extern process bistod reklamation")]
        TerminatedTrialConfirmedComplaint = 6,
        [Description("Extern process avslog reklamation")]
        TerminatedTrialDeniedComplaint = 7,
    }
}
