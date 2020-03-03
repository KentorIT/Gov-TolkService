using System.Collections.Generic;

namespace Tolk.Api.Payloads.Responses
{
    public class ConfirmUpdateResponse : ResponseBase
    {
        public IEnumerable<ConfirmedUpdateModel> ConfirmedUpdates { get; set; }
    }
}
