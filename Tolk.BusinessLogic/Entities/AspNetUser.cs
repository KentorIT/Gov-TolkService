using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Tolk.BusinessLogic.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class AspNetUser : IdentityUser<int>
    {
        private AspNetUser() { }

        public AspNetUser(string email)
        {
            Email = email;
            UserName = email;
        }

        public AspNetUser(string email, CustomerOrganisation customer)
            : this(email)
        {
            CustomerOrganisation = customer;
        }

        public AspNetUser(string email, Broker broker)
            :this(email)
        {
            Broker = broker;
        }

        public List<IdentityUserRole<int>> Roles { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }
    
        public int? BrokerId { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public int? CustomerOrganisationId { get; set; }

        public int? InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }

        public static AspNetUser CreateInterpreter(string email)
        {
            var user = new AspNetUser(email);

            user.Interpreter = new Interpreter()
            {
                // Add empty list because other code expects initialized entity.
                Brokers = new List<InterpreterBroker>()
            };

            return user;
        }
    }
}
