using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrokerMock.Helpers
{
    public class BrokerMockOptions
    {
        public string TolkApiBaseUrl { get; set; }

        public bool UseSecret { get; set; }
        public string Secret { get; set; }
    }
}
