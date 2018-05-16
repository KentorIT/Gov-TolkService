using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Services
{
    public static class Policies
    {
        public const string Customer = nameof(Customer);
        public const string Broker = nameof(Broker);
        public const string Interpreter = nameof(Interpreter);

        public static void RegisterTolkAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Customer, conf => conf.RequireClaim(TolkClaimTypes.CustomerOrganisationId));
                opt.AddPolicy(Broker, conf => conf.RequireClaim(TolkClaimTypes.BrokerId));
                opt.AddPolicy(Interpreter, conf => conf.RequireClaim(TolkClaimTypes.InterpreterId));
            });
        }
    }
}
