using System.Collections.Generic;

namespace Tolk.Api.Payloads.Responses
{
    public class ConfirmChangeResponse : ResponseBase
    {
        public IEnumerable<ConfirmedChangeModel> ConfirmedChanges { get; set; }
    }
}
