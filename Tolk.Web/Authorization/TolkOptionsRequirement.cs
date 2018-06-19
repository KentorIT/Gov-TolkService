using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.Web.Authorization
{
    public abstract class TolkOptionsRequirement : IAuthorizationRequirement
    {
        public abstract bool Evaluate(TolkOptions options);
    }

    public class TolkOptionsRequirement<T> : TolkOptionsRequirement
    {
        public Func<TolkOptions, T> OptionSelector { get; }
        public T ValidValue { get; }

        public TolkOptionsRequirement(Func<TolkOptions, T> optionSelector, T validValue)
        {
            OptionSelector = optionSelector;
            ValidValue = validValue;
        }
        public override bool Evaluate(TolkOptions options)
        {
            return OptionSelector(options).Equals(ValidValue);
        }
    }

    public class TolkOptionsRequirementHandler : AuthorizationHandler<TolkOptionsRequirement>
    {
        private TolkOptions _options;

        public TolkOptionsRequirementHandler(IOptions<TolkOptions> options)
        {
            _options = options.Value;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TolkOptionsRequirement requirement)
        {
            if (requirement.Evaluate(_options))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }

}
