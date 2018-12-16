using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerMock.Helpers
{
    public class BrokerMockOptions
    {
        public string TolkApiBaseUrl { get; set; }

        public bool UseApiKey { get; set; }
        public string ApiUserName { get; set; }
        public string ApiKey { get; set; }
    }
}
