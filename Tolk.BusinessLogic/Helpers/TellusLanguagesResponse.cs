using System.Collections.Generic;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusLanguagesResponse
    {
        public int Status { get; set; }
        public IEnumerable<TellusLanguageModel> Result { get; set; }
    }
}
