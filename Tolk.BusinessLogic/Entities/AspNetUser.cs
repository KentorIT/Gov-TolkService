using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Tolk.BusinessLogic.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class AspNetUser : IdentityUser
    {
        public List<IdentityUserRole<string>> Roles { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }
    
        public int? BrokerId { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public int? CustomerOrganisationId { get; set; }

        public int? InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }
    }
}
