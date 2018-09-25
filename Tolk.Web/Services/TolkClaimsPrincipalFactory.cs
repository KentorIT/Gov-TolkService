using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Authorization;

namespace Tolk.Web.Services
{
    public class TolkClaimsPrincipalFactory : UserClaimsPrincipalFactory<AspNetUser, IdentityRole<int>>
    {
        private TolkDbContext _dbContext;

        public TolkClaimsPrincipalFactory(
            UserManager<AspNetUser> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            TolkDbContext dbContext) 
            : base(userManager, roleManager, optionsAccessor)
        {
            _dbContext = dbContext;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AspNetUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            if(user.CustomerOrganisationId.HasValue)
            {
                identity.AddClaim(new Claim(TolkClaimTypes.CustomerOrganisationId, user.CustomerOrganisationId.ToString()));
            }

            if(user.BrokerId.HasValue)
            {
                identity.AddClaim(new Claim(TolkClaimTypes.BrokerId, user.BrokerId.ToString()));
            }

            if(user.InterpreterId.HasValue)
            {
                identity.AddClaim(new Claim(TolkClaimTypes.InterpreterId, user.InterpreterId.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                identity.AddClaim(new Claim(TolkClaimTypes.PersonalName, user.FullName));
            }

            return identity;
        }
    }
}
