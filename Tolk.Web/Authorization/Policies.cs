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
        public const string Replace = nameof(Replace);
        public const string TimeTravel = nameof(TimeTravel);
        public const string RenderMenuAndStartPageBoxes = nameof(RenderMenuAndStartPageBoxes);

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
                opt.AddPolicy(Replace, builder => builder.RequireAssertion(ReplaceHandler));
                opt.AddPolicy(RenderMenuAndStartPageBoxes, builder => builder.RequireAssertion(RenderMenuAndStartBoxesHandler));
                opt.AddPolicy(TimeTravel, builder =>
                    builder.RequireRole(Roles.Admin)
                    .AddRequirements(new TolkOptionsRequirement<bool>(o => o.EnableTimeTravel, true)));
            });

            services.AddSingleton<IAuthorizationHandler, TolkOptionsRequirementHandler>();
        }

        private readonly static Func<AuthorizationHandlerContext, bool> RenderMenuAndStartBoxesHandler = (context) =>
        {
            return context.User.HasClaim(c => c.Type == TolkClaimTypes.PersonalName);
        };

        private readonly static Func<AuthorizationHandlerContext, bool> EditHandler = (context) =>
        {
            var user = context.User;
            switch (context.Resource)
            {
                case Order order:
                    return order.CreatedBy == context.User.GetUserId() || order.ContactPersonId == context.User.GetUserId();
                case AspNetUser editedUser:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return user.IsInRole(Roles.SuperUser) && editedUser.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return user.IsInRole(Roles.SuperUser) && editedUser.CustomerOrganisationId == user.GetCustomerOrganisationId();
                    }
                    return user.IsInRole(Roles.Admin);
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> ReplaceHandler = (context) =>
        {
            var user = context.User;
            switch (context.Resource)
            {
                case Order order:
                    return order.CreatedBy == context.User.GetUserId();
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CancelHandler = (context) =>
        {
            switch (context.Resource)
            {
                case Request request:
                    if (context.User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return request.Order.CreatedBy == context.User.GetUserId() &&
                        (request.Order.Status == OrderStatus.Requested || request.Order.Status == OrderStatus.RequestResponded || request.Order.Status == OrderStatus.ResponseAccepted || request.Order.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                        (request.Status == RequestStatus.Created || request.Status == RequestStatus.Received || request.Status == RequestStatus.Accepted || request.Status == RequestStatus.Approved || request.Status == RequestStatus.AcceptedNewInterpreterAppointed);
                    }
                    else if (context.User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return request.Ranking.BrokerId == context.User.GetBrokerId() && request.Status == RequestStatus.Approved && request.Order.Status == OrderStatus.ResponseAccepted;
                    }
                    return false;
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

            int userId = user.GetUserId();

            switch (context.Resource)
            {
                case Order order:
                    return user.IsInRole(Roles.SuperUser) ?
                        order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                        order.CreatedBy == userId || order.ContactPersonId == userId;
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
                        return user.IsInRole(Roles.SuperUser) ?
                            requisition.Request.Order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                            requisition.Request.Order.CreatedBy == userId ||
                            requisition.Request.Order.ContactPersonId == userId;
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
                        return user.IsInRole(Roles.SuperUser) ?
                            request.Order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                            request.Order.CreatedBy == userId ||
                            request.Order.ContactPersonId == userId;
                    }
                    return false;
                case Complaint complaint:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return user.IsInRole(Roles.SuperUser) ?
                            complaint.Request.Order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                            complaint.Request.Order.CreatedBy == userId ||
                            complaint.Request.Order.ContactPersonId == userId;
                    }
                    return false;
                case Attachment attachment:
                    if (!attachment.Requisitions.Any() && !attachment.Requests.Any() && !attachment.Orders.Any())
                    {
                        return userId == attachment.CreatedBy;
                    }
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return attachment.Requisitions.Any(a => a.Requisition.Request.Ranking.BrokerId == user.GetBrokerId()) ||
                            attachment.Requests.Any(a => a.Request.Ranking.BrokerId == user.GetBrokerId()) ||
                            attachment.Orders.Any(o => o.Order.Requests.Any(r => r.Ranking.BrokerId == user.GetBrokerId()));
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        if (user.IsInRole(Roles.SuperUser))
                        {
                            var customerOrganisationId = user.GetCustomerOrganisationId();
                            return attachment.Requisitions.Any(a => a.Requisition.Request.Order.CustomerOrganisationId == customerOrganisationId) ||
                                attachment.Requests.Any(a => a.Request.Order.CustomerOrganisationId == customerOrganisationId) || attachment.Orders.Any(oa => oa.Order.CustomerOrganisationId == customerOrganisationId);
                        }
                        else
                        {
                            return attachment.Requisitions.Any(a =>
                                    a.Requisition.Request.Order.CreatedBy == userId ||
                                    a.Requisition.Request.Order.ContactPersonId == userId) ||
                                attachment.Requests.Any(a => a.Request.Order.CreatedBy == userId ||
                                    a.Request.Order.ContactPersonId == userId) ||
                                     attachment.Orders.Any(oa => oa.Order.CreatedBy == userId || oa.Order.ContactPersonId == userId);
                        }
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.InterpreterId))
                    {
                        return attachment.Requisitions.Any(a => a.Requisition.Request.InterpreterId == user.GetInterpreterId()) ||
                            attachment.Requests.Any(a => a.Request.InterpreterId == user.GetInterpreterId()) ||
                            attachment.Orders.Any(o => o.Order.Requests.Any(r => r.InterpreterId == user.GetInterpreterId()));
                    }
                    return false;
                case AspNetUser viewUser:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return user.IsInRole(Roles.SuperUser) && viewUser.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return user.IsInRole(Roles.SuperUser) && viewUser.CustomerOrganisationId == user.GetCustomerOrganisationId();
                    }
                    return user.IsInRole(Roles.Admin);
                default:
                    throw new NotImplementedException();
            }
        };
    }
}
