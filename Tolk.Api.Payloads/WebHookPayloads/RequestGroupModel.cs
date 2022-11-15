using System;
using System.Collections.Generic;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestGroupModel : RequestBaseModel
    {
        public bool RequireSameInterpreter { get; set; }
        public string OrderGroupNumber { get; set; }
        public IEnumerable<OccasionModel> Occasions { get; set; }
    }
}
