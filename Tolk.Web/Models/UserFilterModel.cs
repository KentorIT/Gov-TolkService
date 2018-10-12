using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UserFilterModel
    {
        [Display(Name = "Namn")]
        public string Name { get; set; }

        [Display(Name = "Organisation")]
        public string OrganisationIdentifier { get; set; }
        
        [Display(Name = "Roll")]
        public SearchableRoles? Roles { get; set; }

        [Display(Name = "Endast aktiva användare")]
        public bool OnlyActive { get; set; } = false;

        internal IQueryable<AspNetUser> Apply(IQueryable<AspNetUser> users, IEnumerable<RoleMap> roles)
        {
            users = !string.IsNullOrWhiteSpace(Name)
               ? users.Where(u => u.NameFirst.Contains(Name) || u.NameFamily.Contains(Name))
               : users;
            if (!string.IsNullOrWhiteSpace(OrganisationIdentifier))
            {
                var org = OrganisationIdentifier.Split("_");
                var id = int.Parse(org.First());
                var type = Enum.Parse<OrganisationType>(org.Last());
                switch (type)
                {
                    case OrganisationType.GovernmentBody:
                        users = users.Where(u => u.CustomerOrganisationId == id);
                        break;
                    case OrganisationType.Broker:
                        users = users.Where(u => u.BrokerId == id);
                        break;
                    case OrganisationType.Owner:
                        users = users.Where(u => !u.BrokerId.HasValue && !u.CustomerOrganisationId.HasValue && !u.InterpreterId.HasValue);
                        break;
                    default:
                        throw new NotSupportedException($"{type.GetDescription()} is not a supported {nameof(OrganisationType)} when searching users.");
                }
            }
            if (Roles.HasValue)
            {
                if ((Roles.Value & SearchableRoles.Broker) == SearchableRoles.Broker)
                    {
                    users = users.Where(u => u.BrokerId != null);
                }
                if ((Roles.Value & SearchableRoles.OrderCreator) == SearchableRoles.OrderCreator)
                {
                    users = users.Where(u => u.CustomerOrganisationId != null);
                }
                if ((Roles.Value & SearchableRoles.Interpreter) == SearchableRoles.Interpreter)
                {
                    users = users.Where(u => u.InterpreterId != null);
                }
                if ((Roles.Value & SearchableRoles.SystemAdministrator) == SearchableRoles.SystemAdministrator)
                {
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == roles.Single(role => role.Name == Authorization.Roles.Admin).Id));
                }
                if ((Roles.Value & SearchableRoles.OrganisationAdministrator) == SearchableRoles.OrganisationAdministrator)
                {
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == roles.Single(role => role.Name == Authorization.Roles.SuperUser).Id) &&
                        (u.CustomerOrganisationId != null || u.BrokerId != null) );
                }
            }
            if (OnlyActive)
            {
                users = users.Where(u => u.IsActive);
            }
            return users;
        }
    }

    public class OrganisationIdentifier
    {
        public OrganisationType OrganisationType { get; set; }

        //This can be the id in difierent tables, CustomerOrganisation or Brokar at this time.
        public int Id { get; set; }
    }

    public enum OrganisationType
    {
        [Description("Myndighet")]
        GovernmentBody = 1,

        [Description("Tolkförmedling")]
        Broker = 2,

        [Description("Systemägare")]
        Owner = 3,
    }

    [Flags]
    public enum SearchableRoles
    {
        [Description("Avropare")]
        OrderCreator = 1,

        [Description("Tolkförmedlare")]
        Broker = 2,

        [Description("Administratör på organisation")]
        OrganisationAdministrator = 4,

        [Description("Tolk")]
        Interpreter = 8,

        [Description("Systemadministratör")]
        SystemAdministrator = 16,
    }
}
