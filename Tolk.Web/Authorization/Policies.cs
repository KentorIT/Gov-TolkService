using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string EditContact = nameof(EditContact);
        public const string CreateRequisition = nameof(CreateRequisition);
        public const string CreateComplaint = nameof(CreateComplaint);
        public const string View = nameof(View);
        public const string Accept = nameof(Accept);
        public const string Cancel = nameof(Cancel);
        public const string Replace = nameof(Replace);
        public const string TimeTravel = nameof(TimeTravel);
        public const string ViewMenuAndStartLists = nameof(ViewMenuAndStartLists);
        public const string HasPassword = nameof(HasPassword);
        public const string CustomerOrAdmin = nameof(CustomerOrAdmin);
        public const string CentralLocalAdminCustomer = nameof(CentralLocalAdminCustomer);
        public const string SystemCentralLocalAdmin = nameof(SystemCentralLocalAdmin);

        public static void RegisterTolkAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Customer, builder => builder.RequireClaim(TolkClaimTypes.CustomerOrganisationId));
                opt.AddPolicy(Broker, builder => builder.RequireClaim(TolkClaimTypes.BrokerId));
                opt.AddPolicy(Interpreter, builder => builder.RequireClaim(TolkClaimTypes.InterpreterId));
                opt.AddPolicy(EditContact, builder => builder.RequireAssertion(EditContactHandler));
                opt.AddPolicy(Edit, builder => builder.RequireAssertion(EditHandler));
                opt.AddPolicy(CreateRequisition, builder => builder.RequireAssertion(CreateRequisitionHandler));
                opt.AddPolicy(CreateComplaint, builder => builder.RequireAssertion(CreateComplaintHandler));
                opt.AddPolicy(View, builder => builder.RequireAssertion(ViewHandler));
                opt.AddPolicy(Accept, builder => builder.RequireAssertion(AcceptHandler));
                opt.AddPolicy(Cancel, builder => builder.RequireAssertion(CancelHandler));
                opt.AddPolicy(Replace, builder => builder.RequireAssertion(ReplaceHandler));
                opt.AddPolicy(ViewMenuAndStartLists, builder => builder.RequireAssertion(ViewMenuAndStartListsHandler));
                opt.AddPolicy(HasPassword, builder => builder.RequireAssertion(HasPasswordHandler));
                opt.AddPolicy(TimeTravel, builder =>
                    builder.RequireRole(Roles.SystemAdministrator)
                    .AddRequirements(new TolkOptionsRequirement<bool>(o => o.EnableTimeTravel, true)));
                opt.AddPolicy(CustomerOrAdmin, builder => builder.RequireAssertion(CustomerOrAdminHandler));
                opt.AddPolicy(CentralLocalAdminCustomer, builder => builder.RequireAssertion(CentralLocalAdminHandler));
                opt.AddPolicy(SystemCentralLocalAdmin, builder => builder.RequireAssertion(SystemCentralLocalAdminHandler));
            });

            services.AddSingleton<IAuthorizationHandler, TolkOptionsRequirementHandler>();
        }

        private readonly static Func<AuthorizationHandlerContext, bool> ViewMenuAndStartListsHandler = (context) =>
        {
            return context.User.HasClaim(c => c.Type == TolkClaimTypes.PersonalName);
        };

        private readonly static Func<AuthorizationHandlerContext, bool> HasPasswordHandler = (context) =>
        {
            return context.User.HasClaim(c => c.Type == TolkClaimTypes.IsPasswordSet);
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CustomerOrAdminHandler = (context) =>
        {
            return context.User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) ||
                context.User.IsInRole(Roles.SystemAdministrator);
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CentralLocalAdminHandler = (context) =>
        {
            return context.User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) &&
               (context.User.IsInRole(Roles.CentralAdministrator) || context.User.HasClaim(c => c.Type == TolkClaimTypes.LocalAdminCustomerUnits));
        };

        private readonly static Func<AuthorizationHandlerContext, bool> SystemCentralLocalAdminHandler = (context) =>
        {
            return (context.User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) && context.User.HasClaim(c => c.Type == TolkClaimTypes.LocalAdminCustomerUnits))
            || context.User.IsInRole(Roles.CentralAdministrator)
            || context.User.IsInRole(Roles.SystemAdministrator);
        };

        private readonly static Func<AuthorizationHandlerContext, bool> EditHandler = (context) =>
        {
            var user = context.User;
            var localAdminCustomerUnits = user.TryGetLocalAdminCustomerUnits();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), user.GetUserId());
                case InterpreterBroker interpreter:
                    return user.IsInRole(Roles.CentralAdministrator) && interpreter.BrokerId == user.TryGetBrokerId();
                case AspNetUser editedUser:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return user.IsInRole(Roles.CentralAdministrator) && editedUser.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        if (user.IsInRole(Roles.CentralAdministrator))
                        {
                            return editedUser.CustomerOrganisationId == user.TryGetCustomerOrganisationId();
                        }
                        //check that edited user has at least one of the same units as users localadminunits
                        else if (localAdminCustomerUnits.Any())
                        {
                            var editedUsersCustomerUnits = editedUser.CustomerUnits.Select(cu => cu.CustomerUnitId);
                            return editedUsersCustomerUnits.Intersect(localAdminCustomerUnits).Any();
                        }
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
                case CustomerOrganisation organisation:
                    return user.IsInRole(Roles.SystemAdministrator);
                case CustomerUnit unit:
                    return (user.IsInRole(Roles.CentralAdministrator) && user.TryGetCustomerOrganisationId() == unit.CustomerOrganisationId) ||
                        IsUserLocalAdminOfCustomerUnit(unit.CustomerUnitId, localAdminCustomerUnits);
                case CustomerUnitUser unituser:
                    return (user.IsInRole(Roles.CentralAdministrator) && user.TryGetCustomerOrganisationId() == unituser.CustomerUnit.CustomerOrganisationId) ||
                        IsUserLocalAdminOfCustomerUnit(unituser.CustomerUnitId, localAdminCustomerUnits);
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> EditContactHandler = (context) =>
        {
            var userId = context.User.GetUserId();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreatorOrContact(context.User.TryGetAllCustomerUnits(), context.User.TryGetCustomerOrganisationId(), userId);
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
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), user.GetUserId());
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CancelHandler = (context) =>
        {
            var user = context.User;
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), user.GetUserId());
                case Request request:
                    return user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId) &&
                        request.Ranking.BrokerId == user.GetBrokerId() &&
                        request.Status == RequestStatus.Approved && request.Order.Status == OrderStatus.ResponseAccepted;
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CreateRequisitionHandler = (context) =>
        {
            switch (context.Resource)
            {
                case Request request:
                    return context.User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId) && request.Ranking.BrokerId == context.User.GetBrokerId();
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CreateComplaintHandler = (context) =>
        {
            var user = context.User;
            switch (context.Resource)
            {
                case Request request:
                    return request.Order.IsAuthorizedAsCreatorOrContact(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), user.GetUserId());
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> AcceptHandler = (context) =>
        {
            var user = context.User;
            int userId = user.GetUserId();
            var customerUnits = user.TryGetAllCustomerUnits();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), userId);
                case Request request:
                    return request.Ranking.BrokerId == user.GetBrokerId();
                case Requisition requisition:
                    return requisition.Request.Order.IsAuthorizedAsCreatorOrContact(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), userId);
                case Complaint complaint:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return complaint.Request.Order.IsAuthorizedAsCreatorOrContact(user.TryGetAllCustomerUnits(), user.TryGetCustomerOrganisationId(), userId);
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
            var localAdminCustomerUnits = user.TryGetLocalAdminCustomerUnits();
            var customerUnits = user.TryGetAllCustomerUnits();

            switch (context.Resource)
            {
                case Order order:
                    return user.IsInRole(Roles.SystemAdministrator) || (user.IsInRole(Roles.CentralAdministrator) ?
                        order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                        order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId));
                case Requisition requisition:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return requisition.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return user.IsInRole(Roles.CentralAdministrator) ?
                            requisition.Request.Order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                            requisition.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId);
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
                case Request request:
                    return user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId) && request.Ranking.BrokerId == user.GetBrokerId();
                case Complaint complaint:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return user.IsInRole(Roles.CentralAdministrator) ?
                            complaint.Request.Order.CustomerOrganisationId == user.GetCustomerOrganisationId() :
                            complaint.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId);
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
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
                        if (user.IsInRole(Roles.CentralAdministrator))
                        {
                            var customerOrganisationId = user.GetCustomerOrganisationId();
                            return attachment.Requisitions.Any(a => a.Requisition.Request.Order.CustomerOrganisationId == customerOrganisationId) ||
                                attachment.Requests.Any(a => a.Request.Order.CustomerOrganisationId == customerOrganisationId) || attachment.Orders.Any(oa => oa.Order.CustomerOrganisationId == customerOrganisationId);
                        }
                        else
                        {
                            return attachment.Requisitions.Any(a => a.Requisition.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId))
                                     || attachment.Requests.Any(ra => ra.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId))
                                     || attachment.Orders.Any(oa => oa.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId));
                        }
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
                case AspNetUser viewUser:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return user.IsInRole(Roles.CentralAdministrator) && viewUser.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return (user.IsInRole(Roles.CentralAdministrator) || localAdminCustomerUnits.Any())
                            && viewUser.CustomerOrganisationId == user.GetCustomerOrganisationId();
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
                case InterpreterBroker interpreter:
                    return user.IsInRole(Roles.CentralAdministrator) && interpreter.BrokerId == user.TryGetBrokerId();
                case CustomerOrganisation organisation:
                    return user.IsInRole(Roles.SystemAdministrator);
                case CustomerUnit unit:
                    return (user.IsInRole(Roles.CentralAdministrator) && unit.CustomerOrganisationId == user.TryGetCustomerOrganisationId()) ||
                        IsUserLocalAdminOfCustomerUnit(unit.CustomerUnitId, localAdminCustomerUnits);
                default:
                    throw new NotImplementedException();
            }
        };

        private static bool IsUserLocalAdminOfCustomerUnit(int customerUnitId, IEnumerable<int> localAdmincustomerUnits)
        {
            return localAdmincustomerUnits.Any() && localAdmincustomerUnits.Contains(customerUnitId);
        }

    }
}
