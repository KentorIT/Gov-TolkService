using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
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
