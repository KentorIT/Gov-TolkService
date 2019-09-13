using System;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Net.Http;
using Tolk.Web.Api.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Services
{
    public class TolkBaseOptionsService : ITolkBaseOptions

    {
        private readonly TolkApiOptions _options;

        public TolkBaseOptionsService(IOptions<TolkApiOptions> options)
        {
            _options = options.Value;
        }

        public TolkBaseOptions.Environment Env => _options.Env;

        public string TolkWebBaseUrl => _options.TolkWebBaseUrl;

        public int MonthsToApproveComplaints => _options.MonthsToApproveComplaints;

        public TolkBaseOptions.TellusApi Tellus => _options.Tellus;

        public TolkBaseOptions.SupportSettings Support => _options.Support;
    }
}
