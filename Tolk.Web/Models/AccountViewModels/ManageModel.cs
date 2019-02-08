using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ManageModel : AccountViewModel
    {
        public bool HasPassword { get; set; }


        
        [ClientRequired]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord", Description = "Bekräfta ändringarna med ditt lösenord")]
        [StringLength(100)]
        public string CurrentPassword { get; set; }

    }
}
