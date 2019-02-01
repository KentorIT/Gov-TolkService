using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ChangeEmailModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord", Description = "Bekräfta ändringen med ditt lösenord")]
        [StringLength(100)]
        public string CurrentPassword { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "Ny e-postadress")]
        [StringLength(255)]
        public string NewEmailAddress { get; set; }
    }
}
