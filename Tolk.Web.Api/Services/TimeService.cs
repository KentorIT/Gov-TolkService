using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Services
{
    public class TimeService : ISwedishClock

    {
        private readonly TolkApiOptions _options;
        private static HttpClient client = new HttpClient();

        public TimeService(IOptions<TolkApiOptions> options)
        {
            _options = options.Value;
        }

        public DateTimeOffset SwedenNow => GetTimeAsync().Result;

        private async Task<DateTimeOffset> GetTimeAsync()
        {
            //Also add cert to call
            var response = await client.GetAsync($"{_options.TolkWebBaseUrl}/Time/");
            return await response.Content.ReadAsAsync<DateTimeOffset>();
        }
    }
}
