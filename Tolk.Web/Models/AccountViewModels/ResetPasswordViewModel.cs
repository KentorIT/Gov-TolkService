using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ResetPasswordViewModel : NewPasswordModelBase
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
