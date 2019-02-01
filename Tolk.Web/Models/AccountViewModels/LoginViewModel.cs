using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "E-post eller användarnamn")]
        public string UserName { get; set; }

        [ClientRequired]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord")]
        [StringLength(100)]
        public string Password { get; set; }
    }
}
