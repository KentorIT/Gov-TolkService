using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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
    public class TolkClaimsPrincipalFactory : UserClaimsPrincipalFactory<AspNetUser>
    {
        private TolkDbContext _dbContext;

        public TolkClaimsPrincipalFactory(
            UserManager<AspNetUser> userManager,
            IOptions<IdentityOptions> optionsAccessor,
            TolkDbContext dbContext) 
            : base(userManager, optionsAccessor)
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

            return identity;
        }
    }

    public static class TolkClaimsPrincipalFactoryExtension
    {
       public static void AddTolkClaimsPrincipalFactory(this IServiceCollection services)
       {

       }
    }
}
