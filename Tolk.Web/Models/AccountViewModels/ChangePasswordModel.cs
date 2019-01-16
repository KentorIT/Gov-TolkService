using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ChangePasswordModel : NewPasswordModelBase
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Befintligt lösenord")]
        [StringLength(100)]
        public string CurrentPassword { get; set; }

        public bool HasPassword { get; set; }
    }
}
