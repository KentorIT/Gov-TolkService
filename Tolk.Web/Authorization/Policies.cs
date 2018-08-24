using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Authorization
{
    public static class Policies
    {
        public const string Customer = nameof(Customer);
        public const string Broker = nameof(Broker);
        public const string Interpreter = nameof(Interpreter);
        public const string Edit = nameof(Edit);
        public const string CreateRequisition = nameof(CreateRequisition);
        public const string CreateComplaint = nameof(CreateComplaint);
        public const string View = nameof(View);
        public const string Accept = nameof(Accept);
        public const string Cancel = nameof(Cancel);
        public const string TimeTravel = nameof(TimeTravel);

        public static void RegisterTolkAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Customer, builder => builder.RequireClaim(TolkClaimTypes.CustomerOrganisationId));
                opt.AddPolicy(Broker, builder => builder.RequireClaim(TolkClaimTypes.BrokerId));
                opt.AddPolicy(Interpreter, builder => builder.RequireClaim(TolkClaimTypes.InterpreterId));
                opt.AddPolicy(Edit, builder => builder.RequireAssertion(EditHandler));
                opt.AddPolicy(CreateRequisition, builder => builder.RequireAssertion(CreateRequisitionHandler));
                opt.AddPolicy(CreateComplaint, builder => builder.RequireAssertion(CreateComplaintHandler));
                opt.AddPolicy(View, builder => builder.RequireAssertion(ViewHandler));
                opt.AddPolicy(Accept, builder => builder.RequireAssertion(AcceptHandler));
                opt.AddPolicy(Cancel, builder => builder.RequireAssertion(CancelHandler));
                opt.AddPolicy(TimeTravel, builder =>
                    builder.RequireRole(Roles.Admin)
                    .AddRequirements(new TolkOptionsRequirement<bool>(o => o.EnableTimeTravel, true)));
            });

            services.AddSingleton<IAuthorizationHandler, TolkOptionsRequirementHandler>();
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

        private readonly static Func<AuthorizationHandlerContext, bool> CancelHandler = (context) =>
        {
            switch (context.Resource)
            {
                case Request request:
                    return request.Order.CreatedBy == context.User.GetUserId() &&
                        (request.Order.Status == OrderStatus.Requested || request.Order.Status == OrderStatus.RequestResponded || request.Order.Status == OrderStatus.ResponseAccepted || request.Order.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                        (request.Status == RequestStatus.Created || request.Status == RequestStatus.Received || request.Status == RequestStatus.Accepted || request.Status == RequestStatus.Approved || request.Status == RequestStatus.AcceptedNewInterpreterAppointed);
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CreateRequisitionHandler = (context) =>
        {
            switch (context.Resource)
            {
                case Request request:
                    if (context.User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return request.Ranking.BrokerId == context.User.GetBrokerId();
                    }
                    else if (context.User.HasClaim(c => c.Type == TolkClaimTypes.InterpreterId))
                    {
                        return request.InterpreterId == context.User.GetInterpreterId();
                    }
                    return false;
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CreateComplaintHandler = (context) =>
        {
            switch (context.Resource)
            {
                case Request request:
                    if (context.User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return request.Order.CreatedBy == context.User.GetUserId() || request.Order.ContactPersonId == context.User.GetUserId();
                    }
                    return false;
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
                case Request request:
                    //TODO: Validate that the has the correct state, is connected to the user
                    return request.Ranking.BrokerId == context.User.GetBrokerId();
                case Requisition requisition:
                    return requisition.Request.Order.CreatedBy == userId || 
                        requisition.Request.Order.ContactPersonId == userId;
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> AcceptHandler = (context) =>
        {
            int userId = context.User.GetUserId();

            switch (context.Resource)
            {
                case Order order:
                    return order.CreatedBy == userId;
                case Request request:
                    //TODO: Validate that the has the correct state, is connected to the user
                    return request.Ranking.BrokerId == context.User.GetBrokerId();
                case Requisition requisition:
                    return requisition.Request.Order.CreatedBy == userId ||
                        requisition.Request.Order.ContactPersonId == userId;
                case Complaint complaint:
                    if (context.User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == context.User.GetBrokerId();
                    }
                    else if (context.User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return complaint.CreatedBy == context.User.GetUserId();
                    }
                    return false;
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> ViewHandler = (context) =>
        {
            var user = context.User;

            switch (context.Resource)
            {
                case Order order:
                    return order.CreatedBy == user.GetUserId();
                case Requisition requisition:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return requisition.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.InterpreterId))
                    {
                        return requisition.Request.InterpreterId == user.GetInterpreterId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return requisition.Request.Order.CreatedBy == user.GetUserId() ||
                            requisition.Request.Order.ContactPersonId == user.GetUserId();
                    }
                    return false;
                case Request request:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.InterpreterId))
                    {
                        return request.InterpreterId == user.GetInterpreterId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return request.Order.CreatedBy == user.GetUserId();
                    }
                    return false;
                case Complaint complaint:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return complaint.Request.Order.CreatedBy == context.User.GetUserId() || complaint.Request.Order.ContactPersonId == context.User.GetUserId();
                    }
                    return false;
                default:
                    throw new NotImplementedException();
            }
        };
    }
}
