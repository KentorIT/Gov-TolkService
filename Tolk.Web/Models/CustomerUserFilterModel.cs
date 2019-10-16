using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class CustomerUserFilterModel
    {
        public int Id { get; set; }

        [Display(Name = "Sök")]
        [Placeholder("Söker på delar av förnamn, efternamn eller epostadress...")]
        public string SearchString { get; set; }

        [Display(Name = "Roll")]
        public UserTypes? UserType { get; set; }

        public int CentralAdministratorRoleId { get; set; }

        public int CentralOrderHandlerRoleId { get; set; }
    }
}
