using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class UserCustomerOrganisation
    {
        [Key]
        public string UserId { get; set; }

        public AspNetUser User { get; set; }

        public int CustomerOrganisationId { get; set; }

        public CustomerOrganisation CustomerOrganisation { get; set; }
    }
}
