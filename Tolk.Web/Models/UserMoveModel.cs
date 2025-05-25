using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UserMoveModel
    {
        [Required]
        public int UserId { get; set; }
        
        public int ParentOrganisationId { get; set; }

        [Display(Name = "Namn")]
        public string NameFull => $"{NameFirst} {NameFamily}";

        public string NameFirst { get; set; }

        public string NameFamily { get; set; }

        [Display(Name = "Nuvarande myndighet")]
        public string CurrentCustomerOrganisationName { get; set; }

        public int? CurrentCustomerOrganisationId { get; set; }

        [Display(Name = "Flytta till myndighet")]
        [Required]
        public int? NewCustomerOrganisationId { get; set; }

        public UserMoveValidationModel UserMoveValidationModel { get; set; }

        public UserPageMode UserPageMode { get; set; }
    }
}
