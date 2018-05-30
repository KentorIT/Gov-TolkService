using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Authorization
{
    public static class Policies
    {
        public const string Customer = nameof(Customer);
        public const string Broker = nameof(Broker);
        public const string Interpreter = nameof(Interpreter);
        public const string Edit = nameof(Edit);
        public const string View = nameof(View);
        public const string Approve = nameof(Approve);
        public const string TimeTravel = nameof(TimeTravel);

        public static void RegisterTolkAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Customer, builder => builder.RequireClaim(TolkClaimTypes.CustomerOrganisationId));
                opt.AddPolicy(Broker, builder => builder.RequireClaim(TolkClaimTypes.BrokerId));
                opt.AddPolicy(Interpreter, builder => builder.RequireClaim(TolkClaimTypes.InterpreterId));
                opt.AddPolicy(Edit, builder => builder.RequireAssertion(EditHandler));
                opt.AddPolicy(View, builder => builder.RequireAssertion(CreatorHandler));
                opt.AddPolicy(Approve, builder => builder.RequireAssertion(CreatorHandler));
                opt.AddPolicy(TimeTravel, builder => 
                    builder.AddRequirements(new EnvironmentRequirement("Development"))
                    .RequireAuthenticatedUser());
            });

            services.AddSingleton<IAuthorizationHandler, EnvironmentRequirement.EnvironmentHandler>();
        }

        private readonly static Func<AuthorizationHandlerContext, bool> EditHandler = (context) =>
        {
            switch (context.Resource)
            {
                case Order order:
                    return order.CreatedBy == context.User.GetUserId();
                case Request request:
                    return request.Ranking.BrokerId == context.User.GetBrokerId();
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CreatorHandler = (context) =>
        {
            int userId = context.User.GetUserId();

            switch (context.Resource)
            {
                case Order order:
                    return order.CreatedBy == userId;
                default:
                    throw new NotImplementedException();
            }
        };
    }
}
