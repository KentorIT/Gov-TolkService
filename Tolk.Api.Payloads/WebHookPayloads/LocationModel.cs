
using System;
using System.Collections.Generic;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class LocationModel
    {
        public string Key { get; set; }
        public int Rank { get; set; }
        public string OffsiteContactInformation { get; set; }
        public string Street { get; set; }
        public string City{ get; set; }
    }
}
