using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Helpers
{
    public class UpdateLanguagesCompetenceResult
    {
        public IEnumerable<Language> UpdatedLanguages { get; set; }

        [Display(Name = "Svarsmeddelande")]
        public string Message { get; set; }
    }
}
