using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ChangePasswordModel : NewPasswordModelBase
    {
        [Required]
        [NoAutoComplete]
        [DataType(DataType.Password)]
        [Display(Name = "Befintligt lösenord")]
        [StringLength(100)]
        public string CurrentPassword { get; set; }

        public bool HasPassword { get; set; }

        public string Email { get; set; }
    }
}
