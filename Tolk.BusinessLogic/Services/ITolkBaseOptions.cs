using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public interface ITolkBaseOptions
    {
        Environment Env { get; }
        System.Uri TolkWebBaseUrl { get; }
        int MonthsToApproveComplaints { get; }
        TellusApi Tellus { get; }
        SupportSettings Support { get; }
        SmtpSettings Smtp { get; }
        StatusCheckerSettings StatusChecker { get; }
    }
}
