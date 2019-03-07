using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class TellusConnectionService : ITellusConnection

    {
        private readonly TolkOptions _options;

        public TellusConnectionService(IOptions<TolkOptions> options)
        {
            _options = options.Value;
        }

        public string Uri => _options.Tellus.Uri;
    }
}
