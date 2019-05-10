using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public interface ITolkBaseOptions
    {
        TolkBaseOptions.Environment Env { get; }
        string TolkWebBaseUrl { get; }
        int MonthsToApproveComplaints { get; }
        TolkBaseOptions.TellusApi Tellus { get; }
        string SupportEmail { get; }
    }
}
