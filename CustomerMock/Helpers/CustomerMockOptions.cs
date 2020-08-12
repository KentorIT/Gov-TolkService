using System;

namespace CustomerMock.Helpers
{
    public class CustomerMockOptions
    {
        public Uri TolkApiBaseUrl { get; set; }
        public bool UseApiKey { get; set; }
        public string ApiUserName { get; set; }
        public string ApiKey { get; set; }
    }
}
