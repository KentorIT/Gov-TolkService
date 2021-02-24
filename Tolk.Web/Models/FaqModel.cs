using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class FaqModel : IModel
    {
        public int FaqId { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Fråga", Description = "Ange en vanlig typ av fråga")]
        [Required]
        public string Question { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Svar", Description = "Ange ett generellt svar på frågan")]
        [Required]
        public string Answer { get; set; }

        [Display(Name = "Publicera frågan för användare", Description = "Kryssa ur rutan om du inte vill att den ska publiceras för användarna")]
        public bool IsDisplayed { get; set; }

        [Display(Name = "Visa för följande användargrupper")]
        [RequiredChecked(Min = 1)]
        public CheckboxGroup DisplayForRoles { get; set; }

    }
}
