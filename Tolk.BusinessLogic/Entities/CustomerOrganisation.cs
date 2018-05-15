using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerOrganisation
    {
        public int CustomerOrganisationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        public List<AspNetUser> Users { get; set; }
    }
}
