using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class LoginViewModel : IModel
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
