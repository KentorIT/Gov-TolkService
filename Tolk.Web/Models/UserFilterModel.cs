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
        public UserTypes? Roles { get; set; }

        [Display(Name = "Status")]
        public ActiveStatus? Status { get; set; }

        [Display(Name = "E-postadress")]
        public string Email { get; set; }

        public UserTypes UserType { get; set; }

        public bool IsCustomer { get; set; }

        public bool IsBroker { get; set; }

        internal IQueryable<AspNetUser> Apply(IQueryable<AspNetUser> users, IEnumerable<RoleMap> roles)
        {
            users = !string.IsNullOrWhiteSpace(Email) ? users.Where(u => u.Email.Contains(Email))
               : users;
            users = !string.IsNullOrWhiteSpace(Name)
               ? users.Where(u => u.NameFirst.Contains(Name) || u.NameFamily.Contains(Name) || (u.NameFirst + u.NameFamily).Contains(Name.Replace(" ", "")))
               : users;
            if (!string.IsNullOrWhiteSpace(OrganisationIdentifier))
            {
                var org = OrganisationIdentifier.Split("_");
                var id = org.First().ToSwedishInt();
                var type = Enum.Parse<OrganisationType>(org.Last());
                users = type switch
                {
                    OrganisationType.GovernmentBody => users.Where(u => u.CustomerOrganisationId == id),
                    OrganisationType.Broker => users.Where(u => u.BrokerId == id),
                    OrganisationType.Owner => users.Where(u => !u.BrokerId.HasValue && !u.CustomerOrganisationId.HasValue && !u.InterpreterId.HasValue),
                    _ => throw new NotSupportedException($"{type.GetDescription()} is not a supported {nameof(OrganisationType)} when searching users."),
                };
            }
            if (Roles.HasValue)
            {
                if ((Roles.Value & UserTypes.Broker) == UserTypes.Broker)
                {
                    users = users.Where(u => u.BrokerId != null);
                }
                if ((Roles.Value & UserTypes.OrderCreator) == UserTypes.OrderCreator)
                {
                    users = users.Where(u => u.CustomerOrganisationId != null);
                }
                if ((Roles.Value & UserTypes.Interpreter) == UserTypes.Interpreter)
                {
                    users = users.Where(u => u.InterpreterId != null);
                }
                if ((Roles.Value & UserTypes.SystemAdministrator) == UserTypes.SystemAdministrator)
                {
                    var rollId = roles.Single(role => role.Name == Authorization.Roles.SystemAdministrator).Id;
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == rollId));
                }
                if ((Roles.Value & UserTypes.ApplicationAdministrator) == UserTypes.ApplicationAdministrator)
                {
                    var rollId = roles.Single(role => role.Name == Authorization.Roles.ApplicationAdministrator).Id;
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == rollId));
                }
                if ((Roles.Value & UserTypes.Impersonator) == UserTypes.Impersonator)
                {
                    var rollId = roles.Single(role => role.Name == Authorization.Roles.Impersonator).Id;
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == rollId));
                }
                if ((Roles.Value & UserTypes.OrganisationAdministrator) == UserTypes.OrganisationAdministrator)
                {
                    var rollId = roles.Single(role => role.Name == Authorization.Roles.CentralAdministrator).Id;
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == rollId) && (u.CustomerOrganisationId != null || u.BrokerId != null));
                }
                if ((Roles.Value & UserTypes.CentralOrderHandler) == UserTypes.CentralOrderHandler)
                {
                    var rollId = roles.Single(role => role.Name == Authorization.Roles.CentralOrderHandler).Id;
                    users = users.Where(u => u.Roles.Any(r => r.RoleId == rollId) && (u.CustomerOrganisationId != null));
                }
            }
            if (Status.HasValue)
            {
                users = users.Where(u => u.IsActive == (Status.Value == ActiveStatus.Active));
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
