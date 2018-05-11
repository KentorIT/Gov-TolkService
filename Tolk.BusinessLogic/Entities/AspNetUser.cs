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

        public UserBroker Broker { get; set; }
        public UserCustomerOrganisation CustomerOrganisation { get; set; }

        public List<InterpreterBrokerRegion> BrokerRegions { get; set; }
    }
}
