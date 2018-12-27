using System;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Net.Http;
using Tolk.Web.Api.Helpers;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Api.Services
{
    public class TimeService : ISwedishClock

    {
        private readonly TolkApiOptions _options;

        public TimeService(IOptions<TolkApiOptions> options)
        {
            _options = options.Value;
        }

        public DateTimeOffset SwedenNow => GetTimeAsync().Result;

        private async Task<DateTimeOffset> GetTimeAsync()
        {
            //Also add cert to call
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync($"{_options.TolkWebBaseUrl}/Time/");
                return await response.Content.ReadAsAsync<DateTimeOffset>();
            }
        }
    }
}
