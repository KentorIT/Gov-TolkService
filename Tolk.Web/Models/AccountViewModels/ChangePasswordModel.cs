using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ChangePasswordModel : NewPasswordModelBase
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Befintligt lösenord")]
        public string CurrentPassword { get; set; }

        public bool HasPassword { get; set; }
    }
}
