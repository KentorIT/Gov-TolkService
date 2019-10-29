using System;

namespace BrokerMock.Helpers
{
    public class BrokerMockOptions
    {
        public Uri TolkApiBaseUrl { get; set; }

        public bool UseApiKey { get; set; }
        public string ApiUserName { get; set; }
        public string ApiKey { get; set; }
    }
}
