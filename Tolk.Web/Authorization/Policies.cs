using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.Web.Helpers;

namespace Tolk.Web.Authorization
{
    public static class Policies
    {
        public const string Customer = nameof(Customer);
        public const string Broker = nameof(Broker);
        public const string Interpreter = nameof(Interpreter);
        public const string Edit = nameof(Edit);
        public const string Connect = nameof(Connect);
        public const string EditDefaultSettings = nameof(EditDefaultSettings);
        public const string EditContact = nameof(EditContact);
        public const string CreateRequisition = nameof(CreateRequisition);
        public const string CreateComplaint = nameof(CreateComplaint);
        public const string View = nameof(View);
        public const string ViewDefaultSettings = nameof(ViewDefaultSettings);
        public const string Accept = nameof(Accept);
        public const string Cancel = nameof(Cancel);
        public const string Replace = nameof(Replace);
        public const string Print = nameof(Print);
        public const string TimeTravel = nameof(TimeTravel);
        public const string ViewMenuAndStartLists = nameof(ViewMenuAndStartLists);
        public const string HasPassword = nameof(HasPassword);
        public const string CustomerOrAdmin = nameof(CustomerOrAdmin);
        public const string CentralLocalAdminCustomer = nameof(CentralLocalAdminCustomer);
        public const string ApplicationAdminOrBrokerCA = nameof(ApplicationAdminOrBrokerCA);
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
                opt.AddPolicy(Connect, builder => builder.RequireAssertion(ConnectHandler));
                opt.AddPolicy(EditDefaultSettings, builder => builder.RequireAssertion(EditDefaultSettingsHandler));
                opt.AddPolicy(CreateRequisition, builder => builder.RequireAssertion(CreateRequisitionHandler));
                opt.AddPolicy(CreateComplaint, builder => builder.RequireAssertion(CreateComplaintHandler));
                opt.AddPolicy(View, builder => builder.RequireAssertion(ViewHandler));
                opt.AddPolicy(ViewDefaultSettings, builder => builder.RequireAssertion(ViewDefaultSettingsHandler));
                opt.AddPolicy(Accept, builder => builder.RequireAssertion(AcceptHandler));
                opt.AddPolicy(Cancel, builder => builder.RequireAssertion(CancelHandler));
                opt.AddPolicy(Replace, builder => builder.RequireAssertion(ReplaceHandler));
                opt.AddPolicy(Print, builder => builder.RequireAssertion(PrintHandler));
                opt.AddPolicy(ViewMenuAndStartLists, builder => builder.RequireAssertion(ViewMenuAndStartListsHandler));
                opt.AddPolicy(HasPassword, builder => builder.RequireAssertion(HasPasswordHandler));
                opt.AddPolicy(TimeTravel, builder =>
                    builder.RequireRole(Roles.SystemAdministrator)
                    .AddRequirements(new TolkOptionsRequirement<bool>(o => o.EnableTimeTravel, true)));
                opt.AddPolicy(CustomerOrAdmin, builder => builder.RequireAssertion(CustomerOrAdminHandler));
                opt.AddPolicy(ApplicationAdminOrBrokerCA, builder => builder.RequireAssertion(ApplicationAdminOrBrokerCentralAdminHandler));
                opt.AddPolicy(CentralLocalAdminCustomer, builder => builder.RequireAssertion(CentralLocalAdminHandler));
                opt.AddPolicy(SystemCentralLocalAdmin, builder => builder.RequireAssertion(SystemCentralLocalAdminHandler));
            });

            services.AddSingleton<IAuthorizationHandler, TolkOptionsRequirementHandler>();
        }

        private readonly static Func<AuthorizationHandlerContext, bool> ViewMenuAndStartListsHandler = (context) =>
        {
            return context.User.HasClaim(c => c.Type == TolkClaimTypes.IsPasswordSet) || context.User.IsImpersonated();
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


        private readonly static Func<AuthorizationHandlerContext, bool> ApplicationAdminOrBrokerCentralAdminHandler = (context) =>
        {
            return (context.User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId) && context.User.IsInRole(Roles.CentralAdministrator)) ||
                context.User.IsInRole(Roles.ApplicationAdministrator);
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
            || context.User.IsInRole(Roles.SystemAdministrator)
            || context.User.IsInRole(Roles.ApplicationAdministrator);
        };

        private readonly static Func<AuthorizationHandlerContext, bool> EditHandler = (context) =>
        {
            var user = context.User;
            var localAdminCustomerUnits = user.TryGetLocalAdminCustomerUnits();
            var customerOrganisationId = user.TryGetCustomerOrganisationId();
            switch (context.Resource)
            {
                case OrderGroup orderGroup:
                    return orderGroup.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralOrderHandler));
                case Order order:
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralOrderHandler));
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
                    return user.IsInRole(Roles.SystemAdministrator) || user.IsInRole(Roles.ApplicationAdministrator);
                case CustomerOrganisation organisation:
                    return user.IsInRole(Roles.ApplicationAdministrator);
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

        private readonly static Func<AuthorizationHandlerContext, bool> EditDefaultSettingsHandler = (context) =>
        {
            var user = context.User;
            var localAdminCustomerUnits = user.TryGetLocalAdminCustomerUnits();
            switch (context.Resource)
            {
                case AspNetUser editedUser:
                    return user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) && editedUser.Id == user.GetUserId();
                case CustomerOrganisation organisation:
                    return user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) && user.IsInRole(Roles.CentralAdministrator) && user.TryGetCustomerOrganisationId() == organisation.CustomerOrganisationId;
                case CustomerUnit unit:
                    return (user.IsInRole(Roles.CentralAdministrator) && user.TryGetCustomerOrganisationId() == unit.CustomerOrganisationId) ||
                        IsUserLocalAdminOfCustomerUnit(unit.CustomerUnitId, localAdminCustomerUnits);
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> EditContactHandler = (context) =>
        {
            var userId = context.User.GetUserId();
            var customerOrganisationId = context.User.TryGetCustomerOrganisationId();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreatorOrContact(context.User.TryGetAllCustomerUnits(), customerOrganisationId, userId, context.User.IsInRole(Roles.CentralOrderHandler));
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> ReplaceHandler = (context) =>
        {
            var user = context.User;
            var customerOrganisationId = user.TryGetCustomerOrganisationId();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralOrderHandler));
                case OutboundWebHookCall webHookCall:
                    return user.IsInRole(Roles.CentralAdministrator) &&
                        !webHookCall.ResentHookId.HasValue &&
                        webHookCall.RecipientUser.BrokerId == user.TryGetBrokerId();
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> PrintHandler = (context) =>
        {
            var user = context.User;
            var customerOrganisationId = user.TryGetCustomerOrganisationId();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreatorOrContact(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler));
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> CancelHandler = (context) =>
        {
            var user = context.User;
            var customerOrganisationId = user.TryGetCustomerOrganisationId();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralOrderHandler));
                case OrderGroup orderGroup:
                    return orderGroup.IsAuthorizedAsCreator(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralOrderHandler));
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
            var customerOrganisationId = user.TryGetCustomerOrganisationId();
            switch (context.Resource)
            {
                case Request request:
                    return request.Order.IsAuthorizedAsCreatorOrContact(user.TryGetAllCustomerUnits(), customerOrganisationId, user.GetUserId(), user.IsInRole(Roles.CentralOrderHandler));
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> AcceptHandler = (context) =>
        {
            var user = context.User;
            int userId = user.GetUserId();
            var customerOrganisationId = user.TryGetCustomerOrganisationId();
            var customerUnits = user.TryGetAllCustomerUnits();
            switch (context.Resource)
            {
                case Order order:
                    return order.IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, user.IsInRole(Roles.CentralOrderHandler));
                case OrderGroup orderGroup:
                    return orderGroup.IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, user.IsInRole(Roles.CentralOrderHandler));
                case RequestBase request:
                    return request.Ranking.BrokerId == user.GetBrokerId();
                case Requisition requisition:
                    return requisition.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, customerOrganisationId, userId, user.IsInRole(Roles.CentralOrderHandler));
                case Complaint complaint:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return complaint.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, customerOrganisationId, userId, user.IsInRole(Roles.CentralOrderHandler));
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
                    return user.IsInRole(Roles.SystemAdministrator) ||
                        order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler));
                case OrderGroup orderGroup:
                    return user.IsInRole(Roles.SystemAdministrator) ||
                        orderGroup.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler));
                case Requisition requisition:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return requisition.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return requisition.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler));
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
                case Request request:
                    return (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId) && request.Ranking.BrokerId == user.GetBrokerId()) ||
                        (user.HasClaim(c => c.Type == TolkClaimTypes.InterpreterId) && request.Interpreter.InterpreterId == user.GetInterpreterId());
                case RequestGroup requestGroup:
                    return (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId) && requestGroup.Ranking.BrokerId == user.GetBrokerId());
                case Complaint complaint:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return complaint.Request.Ranking.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return complaint.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler));
                    }
                    return user.IsInRole(Roles.SystemAdministrator);
                case Attachment attachment:
                    if (!attachment.Requisitions.Any() && !attachment.Requests.Any() && !attachment.Orders.Any() && !attachment.RequestGroups.Any() && !attachment.OrderGroups.Any())
                    {
                        return userId == attachment.CreatedBy;
                    }
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return attachment.Requisitions.Any(a => a.Requisition.Request.Ranking.BrokerId == user.GetBrokerId()) ||
                            attachment.Requests.Any(a => a.Request.Ranking.BrokerId == user.GetBrokerId()) ||
                            attachment.Orders.Any(o => o.Order.Requests.Any(r => r.Ranking.BrokerId == user.GetBrokerId())) ||
                            attachment.OrderGroups.Any(o => o.OrderGroup.RequestGroups.Any(r => r.Ranking.BrokerId == user.GetBrokerId())) ||
                            attachment.RequestGroups.Any(o => o.RequestGroup.Ranking.BrokerId == user.GetBrokerId());
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        return attachment.Requisitions.Any(a => a.Requisition.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler)))
                                 || attachment.Requests.Any(ra => ra.Request.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler)))
                                 || attachment.Orders.Any(oa => oa.Order.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler)))
                                 || attachment.OrderGroups.Any(o => o.OrderGroup.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler)))
                                 || attachment.RequestGroups.Any(ra => ra.RequestGroup.OrderGroup.IsAuthorizedAsCreatorOrContact(customerUnits, user.GetCustomerOrganisationId(), userId, user.IsInRole(Roles.CentralAdministrator) || user.IsInRole(Roles.CentralOrderHandler)));
                    }
                    return user.IsInRole(Roles.SystemAdministrator) || user.IsInRole(Roles.ApplicationAdministrator);
                case AspNetUser viewUser:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.BrokerId))
                    {
                        return user.IsInRole(Roles.CentralAdministrator) && viewUser.BrokerId == user.GetBrokerId();
                    }
                    else if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        if (user.IsInRole(Roles.CentralAdministrator))
                        {
                            return viewUser.CustomerOrganisationId == user.TryGetCustomerOrganisationId();
                        }
                        //check that viewed user has at least one of the same units as users localadminunits
                        else if (localAdminCustomerUnits.Any())
                        {
                            var editedUsersCustomerUnits = viewUser.CustomerUnits.Select(cu => cu.CustomerUnitId);
                            return editedUsersCustomerUnits.Intersect(localAdminCustomerUnits).Any();
                        }
                    }
                    return user.IsInRole(Roles.SystemAdministrator) || user.IsInRole(Roles.ApplicationAdministrator);
                case InterpreterBroker interpreter:
                    return user.IsInRole(Roles.CentralAdministrator) && interpreter.BrokerId == user.TryGetBrokerId();
                case CustomerOrganisation organisation:
                    return user.IsInRole(Roles.ApplicationAdministrator);
                case CustomerUnit unit:
                    return (user.IsInRole(Roles.CentralAdministrator) && unit.CustomerOrganisationId == user.TryGetCustomerOrganisationId()) ||
                        IsUserLocalAdminOfCustomerUnit(unit.CustomerUnitId, localAdminCustomerUnits);
                case StatusVerificationResult statusModel:
                    return user.IsInRole(Roles.ApplicationAdministrator);
                case OutboundWebHookCall webHookCall:
                    return user.IsInRole(Roles.ApplicationAdministrator) || webHookCall.RecipientUser.BrokerId == user.TryGetBrokerId();
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> ViewDefaultSettingsHandler = (context) =>
        {
            var user = context.User;
            var localAdminCustomerUnits = user.TryGetLocalAdminCustomerUnits();
            switch (context.Resource)
            {
                case AspNetUser viewedUser:
                    if (user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                    {
                        if (user.IsInRole(Roles.CentralAdministrator))
                        {
                            return viewedUser.CustomerOrganisationId == user.TryGetCustomerOrganisationId();
                        }
                        //check that viewed user has at least one of the same units as users localadminunits
                        else if (localAdminCustomerUnits.Any())
                        {
                            var editedUsersCustomerUnits = viewedUser.CustomerUnits.Select(cu => cu.CustomerUnitId);
                            return editedUsersCustomerUnits.Intersect(localAdminCustomerUnits).Any();
                        }
                    }
                    return viewedUser.CustomerOrganisationId.HasValue && (user.IsInRole(Roles.SystemAdministrator) || user.IsInRole(Roles.ApplicationAdministrator));
                case CustomerOrganisation organisation:
                    return user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) && user.IsInRole(Roles.CentralAdministrator) && user.TryGetCustomerOrganisationId() == organisation.CustomerOrganisationId;
                case CustomerUnit unit:
                    return (user.IsInRole(Roles.CentralAdministrator) && user.TryGetCustomerOrganisationId() == unit.CustomerOrganisationId) ||
                        IsUserLocalAdminOfCustomerUnit(unit.CustomerUnitId, localAdminCustomerUnits);
                default:
                    throw new NotImplementedException();
            }
        };

        private readonly static Func<AuthorizationHandlerContext, bool> ConnectHandler = (context) =>
        {
            var user = context.User;
            var localAdminCustomerUnits = user.TryGetLocalAdminCustomerUnits();
            switch (context.Resource)
            {
                case AspNetUser userToBeconnected:
                    return user.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId) && (user.IsInRole(Roles.CentralAdministrator) || localAdminCustomerUnits.Any()) && userToBeconnected.CustomerOrganisationId == user.TryGetCustomerOrganisationId();
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
