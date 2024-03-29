﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;

namespace Tolk.Web.Services
{
    public class TolkClaimsPrincipalFactory : UserClaimsPrincipalFactory<AspNetUser, IdentityRole<int>>
    {
        private readonly TolkDbContext _dbContext;

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
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var identity = await base.GenerateClaimsAsync(user);

            if (user.CustomerOrganisationId.HasValue)
            {
                identity.AddClaim(new Claim(TolkClaimTypes.CustomerOrganisationId, user.CustomerOrganisationId.ToString()));
            }

            if (user.BrokerId.HasValue)
            {
                identity.AddClaim(new Claim(TolkClaimTypes.BrokerId, user.BrokerId.ToString()));
            }

            if (user.InterpreterId.HasValue)
            {
                identity.AddClaim(new Claim(TolkClaimTypes.InterpreterId, user.InterpreterId.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                identity.AddClaim(new Claim(TolkClaimTypes.PersonalName, user.FullName));
            }

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                identity.AddClaim(new Claim(TolkClaimTypes.IsPasswordSet, true.ToString()));
            }

            if (user.CustomerOrganisationId.HasValue)
            {
                var customerUnits = _dbContext.CustomerUnitUsers.Where(cu => cu.UserId == user.Id);
                if (customerUnits.Any())
                {
                    foreach (CustomerUnitUser cu in customerUnits)
                    {
                        identity.AddClaim(new Claim(TolkClaimTypes.AllCustomerUnits, cu.CustomerUnitId.ToSwedishString()));
                        if (cu.IsLocalAdmin)
                        {
                            identity.AddClaim(new Claim(TolkClaimTypes.LocalAdminCustomerUnits, cu.CustomerUnitId.ToSwedishString()));
                        }
                    }
                }
            }

            return identity;
        }
    }
}
