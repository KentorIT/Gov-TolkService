using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public abstract class NewPasswordModelBase : IModel
    {
        [ClientRequired]
        [NoAutoComplete]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [PasswordValidation(
            MinimumPasswordLength = 8,
            MustContainLower = true,
            MustContainUpper = true,
            MustContainNumbers = true,
            MustContainNonAlphanumeric = true)]
        [Display(Name = "Nytt lösenord", Description = "Lösenord ska vara minst 8 tecken långt och innehålla minst 1 liten bokstav, 1 stor bokstav, 1 siffra och 1 symbol/specialtecken.")]
        public string NewPassword { get; set; }

        [ClientRequired]
        [NoAutoComplete]
        [DataType(DataType.Password)]
        [Display(Name = "Bekräfta nytt lösenord")]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }
    }
}
