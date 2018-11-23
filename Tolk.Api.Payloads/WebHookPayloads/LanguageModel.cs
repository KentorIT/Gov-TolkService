
using System;
using System.Collections.Generic;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class LanguageModel
    {
        public string Key { get; set; }
        public string Dialect { get; set; }
        public string Description { get; set; }
        public bool DialectIsRequired { get; set; }
    }
}
