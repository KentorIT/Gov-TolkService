using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class CustomerUserFilterModel
    {
        public int Id { get; set; }

        [Display(Name = "Sök användare")]
        [Placeholder("Söker på delar av förnamn, efternamn eller e-postadress")]
        public string SearchString { get; set; }

        [Display(Name = "Användares roll")]
        public UserTypes? UserType { get; set; }

        public int CentralAdministratorRoleId { get; set; }

        public int CentralOrderHandlerRoleId { get; set; }
    }
}
