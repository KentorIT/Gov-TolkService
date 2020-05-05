using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class FaqFilterModel
    {
        [Display(Name = "Innehåll i fråga/svar")]
        [Placeholder("Sök på del av text")]
        public string QuestionAnswer { get; set; }

        [Display(Name = "Publicerad")]
        public TrueFalse? IsDisplayed { get; set; }

        [Display(Name = "Visas för")]
        public DisplayUserRole? DisplayedFor { get; set; }

        public bool HasActiveFilters => IsDisplayed.HasValue || !string.IsNullOrWhiteSpace(QuestionAnswer) || DisplayedFor.HasValue;
        
        internal IQueryable<Faq> Apply(IQueryable<Faq> faqs)
        {
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
            faqs = !string.IsNullOrWhiteSpace(QuestionAnswer)
                ? faqs.Where(f => f.Answer.Contains(QuestionAnswer) || f.Question.Contains(QuestionAnswer))
                : faqs;
#pragma warning restore CA1307 // 
            faqs = IsDisplayed.HasValue
                ? faqs.Where(f => f.IsDisplayed == (IsDisplayed == TrueFalse.Yes))
                : faqs;
            faqs = DisplayedFor.HasValue
                ? faqs.Where(f => f.FaqDisplayUserRoles.Any(fr => fr.DisplayUserRole == DisplayedFor.Value))
                : faqs;
            return faqs;
        }

    }
}
