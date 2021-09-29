using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Helpers
{
    public class ValidateTellusLanguageListResult
    {
        public IEnumerable<TellusLanguageModel> NewLanguages { get; set; }
        public IEnumerable<TellusLanguageModel> RemovedLanguages { get; set; }
        [Display(Name = "Felmeddelande")]
        public string ErrorMessage { get; set; }
        public bool FoundChanges => NewLanguages.Any() || RemovedLanguages.Any();
        public bool ResultIsValid => string.IsNullOrEmpty(ErrorMessage);
    }
}
