using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Services
{
    public class TolkBaseOptionsService : ITolkBaseOptions

    {
        private readonly TolkApiOptions _options;

        public TolkBaseOptionsService(IOptions<TolkApiOptions> options)
        {
            _options = options?.Value;
        }

        public Environment Env => _options.Env;

        public System.Uri TolkWebBaseUrl => _options.TolkWebBaseUrl;

        public int MonthsToApproveComplaints => _options.MonthsToApproveComplaints;

        public TellusApi Tellus => _options.Tellus;

        public SupportSettings Support => _options.Support;

        public SmtpSettings Smtp => _options.Smtp;

        public StatusCheckerSettings StatusChecker => _options.StatusChecker;

        public bool RunEntityScheduler => false;
        
        public bool AllowDeclineExtraInterpreterOnRequestGroups => _options.AllowDeclineExtraInterpreterOnRequestGroups;

        public bool RoundPriceDecimals => _options.RoundPriceDecimals;
    }
}
