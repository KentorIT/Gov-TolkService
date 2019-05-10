using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Helpers
{
    public class ValidateTellusLanguageListResult
    {
        public IEnumerable<TellusLanguageModel> NewLanguages { get; set; }
        public IEnumerable<TellusLanguageModel> RemovedLanguages { get; set; }

        public bool FoundChanges => NewLanguages.Any() || RemovedLanguages.Any();
    }
}
