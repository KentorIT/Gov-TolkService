using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public UserType? Roles { get; set; }

        [Display(Name = "Status")]
        public ActiveStatus? Status { get; set; }

        public string Email { get; set; }

        public UserType UserType { get; set; }

        public bool HasActiveFilters => !string.IsNullOrWhiteSpace(OrganisationIdentifier) || !string.IsNullOrWhiteSpace(Name) || Roles.HasValue || Status.HasValue;

        internal IQueryable<AspNetUser> Apply(IQueryable<AspNetUser> users, IEnumerable<RoleMap> roles)
        {
            //used when user is created to display only the created user
            users = !string.IsNullOrWhiteSpace(Email) ? users.Where(u => u.Email == Email)
               : users;

            users = !string.IsNullOrWhiteSpace(Name)
               ? users.Where(u => u.NameFirst.Contains(Name) || u.NameFamily.Contains(Name) || (u.NameFirst + u.NameFamily).Contains(Name.Replace(" ", "")))
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
                if ((Roles.Value & UserType.Broker) == UserType.Broker)
                {
                    users = users.Where(u => u.BrokerId != null);
                }
                if ((Roles.Value & UserType.OrderCreator) == UserType.OrderCreator)
                {
                    users = users.Where(u => u.CustomerOrganisationId != null);
                }
                if ((Roles.Value & UserType.Interpreter) == UserType.Interpreter)
                {
                    users = users.Where(u => u.InterpreterId != null);
                }
                if ((Roles.Value & UserType.SystemAdministrator) == UserType.SystemAdministrator)
                {
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == roles.Single(role => role.Name == Authorization.Roles.SystemAdministrator).Id));
                }
                if ((Roles.Value & UserType.ApplicationAdministrator) == UserType.ApplicationAdministrator)
                {
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == roles.Single(role => role.Name == Authorization.Roles.ApplicationAdministrator).Id));
                }
                if ((Roles.Value & UserType.Impersonator) == UserType.Impersonator)
                {
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == roles.Single(role => role.Name == Authorization.Roles.Impersonator).Id));
                }
                if ((Roles.Value & UserType.OrganisationAdministrator) == UserType.OrganisationAdministrator)
                {
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == roles.Single(role => role.Name == Authorization.Roles.CentralAdministrator).Id) &&
                        (u.CustomerOrganisationId != null || u.BrokerId != null));
                }
            }
            if (Status.HasValue)
            {
                if (Status.Value == ActiveStatus.Active)
                {
                    users = users.Where(u => u.IsActive);
                }
                else
                {
                    users = users.Where(u => !u.IsActive);
                }
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

}
