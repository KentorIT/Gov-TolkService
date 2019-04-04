using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class TolkBaseOptionsService : ITolkBaseOptions

    {
        private readonly TolkOptions _options;

        public TolkBaseOptionsService(IOptions<TolkOptions> options)
        {
            _options = options.Value;
        }

        public TolkBaseOptions.Environment Env => _options.Env;

        public string TolkWebBaseUrl => _options.PublicOrigin;

        public int MonthsToApproveComplaints => _options.MonthsToApproveComplaints;

        public TolkBaseOptions.TellusApi Tellus => _options.Tellus;
    }
}
