using System.Collections.Generic;

namespace Tolk.Api.Payloads.Responses
{
    public class ConfirmChangeResponse : ResponseBase
    {
        public string StatusMessage { get; set; }
        public IEnumerable<ConfirmedChangeModel> ConfirmedChanges { get; set; }
    }
}
