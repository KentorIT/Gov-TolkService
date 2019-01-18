using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public abstract class NewPasswordModelBase
    {
        [ClientRequired]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [PasswordValidation(
            MinimumPasswordLength = 8, 
            MustContainLower = true, 
            MustContainUpper = true, 
            MustContainNumbers = true, 
            MustContainNonAlphanumeric = true)]
        [Display(Name = "Nytt lösenord")]
        public string NewPassword { get; set; }

        [ClientRequired]
        [DataType(DataType.Password)]
        [Display(Name = "Bekräfta nytt lösenord")]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }
    }
}
