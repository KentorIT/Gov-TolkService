using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ManageModel : AccountViewModel
    {
        public bool HasPassword { get; set; }

        [ClientRequired]
        [Display(Name = "Förnamn")]
        [StringLength(255)]
        public string NameFirst { get; set; }

        [ClientRequired]
        [Display(Name = "Efternamn")]
        [StringLength(255)]
        public string NameFamily { get; set; }
        
        [ClientRequired]
        [DataType(DataType.Password)]
        [Display(Name = "Lösenord", Description = "Bekräfta ändringarna med ditt lösenord")]
        [StringLength(100)]
        public string CurrentPassword { get; set; }
    }
}
