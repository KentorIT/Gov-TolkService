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

namespace Tolk.Web.Services
{
    public class TolkClaimsPrincipalFactory : UserClaimsPrincipalFactory<AspNetUser, IdentityRole>
    {
        private TolkDbContext _dbContext;

        public TolkClaimsPrincipalFactory(
            UserManager<AspNetUser> userManager,
            RoleManager<IdentityRole> roleManager,
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

            return identity;
        }
    }
}
