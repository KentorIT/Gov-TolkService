using System;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Net.Http;
using Tolk.Web.Api.Helpers;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Api.Services
{
    public class TellusConnectionService : ITellusConnection

    {
        private readonly TolkApiOptions _options;

        public TellusConnectionService(IOptions<TolkApiOptions> options)
        {
            _options = options.Value;
        }

        public string Uri => _options.Tellus.Uri;
    }
}
