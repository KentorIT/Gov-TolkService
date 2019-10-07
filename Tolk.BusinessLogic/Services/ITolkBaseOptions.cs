using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public interface ITolkBaseOptions
    {
        TolkBaseOptions.Environment Env { get; }
        string TolkWebBaseUrl { get; }
        int MonthsToApproveComplaints { get; }
        TolkBaseOptions.TellusApi Tellus { get; }
        TolkBaseOptions.SupportSettings Support { get; }
        TolkBaseOptions.SmtpSettings Smtp { get; }
        TolkBaseOptions.StatusCheckerSettings StatusChecker { get; }
    }
}
