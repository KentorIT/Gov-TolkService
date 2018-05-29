using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Authorization
{
    public class EnvironmentRequirement : IAuthorizationRequirement
    {
        public string EnvironmentName { get; }

        public EnvironmentRequirement(string name)
        {
            EnvironmentName = name;
        }

        public class EnvironmentHandler : AuthorizationHandler<EnvironmentRequirement>
        {
            private readonly IHostingEnvironment _environment;

            public EnvironmentHandler(IHostingEnvironment environment)
            {
                _environment = environment;
            }

            protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EnvironmentRequirement requirement)
            {
                if(_environment.IsEnvironment(requirement.EnvironmentName))
                {
                    context.Succeed(requirement);
                }
                return Task.CompletedTask;
            }
        }
    }
}
