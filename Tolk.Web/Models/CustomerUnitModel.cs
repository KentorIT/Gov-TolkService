﻿using System.ComponentModel.DataAnnotations;
using System;

namespace Tolk.Web.Models
{
    public class CustomerUnitModel
    {
        public int Id { get; set; }

        [Display(Name = "Namn")]
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Felaktig e-postadress")]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "E-post")]
        [StringLength(255)]
        public string Email { get; set; }

        [Display(Name = "Lokal administratör")]
        [Required]
        public int LocalAdministrator { get; set; }

        [Display(Name = "Skapad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Skapad av")]
        public string CreatedBy { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; }

        [Display(Name = "Inkativerad")]
        public DateTimeOffset? InactivatedAt { get; set; }

        [Display(Name = "Inaktiverad av")]
        public string InactivatedBy { get; set; }
    }
}
