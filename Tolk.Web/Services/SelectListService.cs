using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class SelectListService
    {
        private readonly IMemoryCache _cache;
        private readonly TolkDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string languagesSelectListKey = nameof(languagesSelectListKey);
        private const string brokersSelectListKey = nameof(brokersSelectListKey);
        private const string impersonationTargets = nameof(impersonationTargets);
        private const string customersSelectListKey = nameof(customersSelectListKey);
        private const string organisationsSelectListKey = nameof(organisationsSelectListKey);

        public SelectListService(
            IMemoryCache cache,
            TolkDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public static IEnumerable<SelectListItem> Regions { get; } =
            Region.Regions.OrderBy(r => r.Name)
            .Select(r => new SelectListItem
            {
                Value = r.RegionId.ToString(),
                Text = r.Name
            })
            .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> SearchableRoles { get; } =
            EnumHelper.GetAllDescriptions<SearchableRoles>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> ComplaintStatuses { get; } =
            EnumHelper.GetAllDescriptions<ComplaintStatus>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> RequestStatuses { get; } =
            EnumHelper.GetAllDescriptions<RequestStatus>()
                .Where(e => e.Value != RequestStatus.AwaitingDeadlineFromCustomer
                    && e.Value != RequestStatus.NoDeadlineFromCustomer)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> ComplaintTypes { get; } =
            EnumHelper.GetAllDescriptions<ComplaintType>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> RequisitionStatuses { get; } =
            EnumHelper.GetAllDescriptions<RequisitionStatus>()
                .Where(e => e.Value != RequisitionStatus.Approved && e.Value != RequisitionStatus.DeniedByCustomer)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> OrderStatuses { get; } =
            EnumHelper.GetAllDescriptions<OrderStatus>()
            .Where(e => e.Value != OrderStatus.DeliveryAccepted)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> AssignmentStatuses { get; } =
            EnumHelper.GetAllDescriptions<AssignmentStatus>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> AssignmentTypes { get; } =
            EnumHelper.GetAllDescriptions<AssignmentType>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();
        public static IEnumerable<SelectListItem> SystemMessageTypes { get; } =
            EnumHelper.GetAllDescriptions<SystemMessageType>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> SystemMessageUserTypeGroups { get; } =
          EnumHelper.GetAllDescriptions<SystemMessageUserTypeGroup>()
              .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
              .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> Genders { get; } =
            EnumHelper.GetAllDescriptions<Gender>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> BoolList { get; } =
            EnumHelper.GetAllDescriptions<TrueFalse>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> AllowExceedingTravelCost { get; } =
           EnumHelper.GetAllDescriptions<AllowExceedingTravelCost>()
               .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
               .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> InterpreterLocations { get; } =
            EnumHelper.GetAllDescriptions<InterpreterLocation>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> CompetenceLevels { get; } =
            EnumHelper.GetAllDescriptions<CompetenceAndSpecialistLevel>()
                .Where(e => e.Value > 0)
                .OrderByDescending(e => (int)e.Value)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> RequirementTypes { get; } =
            EnumHelper.GetAllDescriptions<RequirementType>()
                .Where(e => e.Value != RequirementType.Dialect)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> TaxCards { get; } =
            EnumHelper.GetAllDescriptions<TaxCard>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> DesireTypes { get; } =
            EnumHelper.GetAllDescriptions<DesireType>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public IEnumerable<SelectListItem> Languages
        {
            get
            {
                if (!_cache.TryGetValue(languagesSelectListKey, out IEnumerable<SelectListItem> items))
                {
                    items = _dbContext.Languages.Where(l => l.Active == true)
                        .OrderBy(l => l.Name).Select(l => new ExtendedSelectListItem
                        {
                            Value = l.LanguageId.ToString(),
                            Text = l.Name,
                            AdditionalDataAttribute = l.TellusName ?? string.Empty
                        })
                    .ToList().AsReadOnly();

                    _cache.Set(languagesSelectListKey, items, DateTimeOffset.Now.AddMinutes(15));
                }

                return items;
            }
        }

        public IEnumerable<SelectListItem> Brokers
        {
            get
            {
                if (!_cache.TryGetValue(brokersSelectListKey, out IEnumerable<SelectListItem> items))
                {
                    items = _dbContext.Brokers.OrderBy(b => b.Name)
                        .Select(b => new SelectListItem
                        {
                            Text = b.Name,
                            Value = b.BrokerId.ToString(),
                        })
                    .ToList().AsReadOnly();

                    _cache.Set(brokersSelectListKey, items, DateTimeOffset.Now.AddMinutes(15));
                }

                return items;
            }
        }

        public IEnumerable<SelectListItem> Organisations
        {
            get
            {
                if (!_cache.TryGetValue(organisationsSelectListKey, out IEnumerable<SelectListItem> items))
                {
                    items = _dbContext.CustomerOrganisations.OrderBy(c => c.Name)
                        .Select(c => new SelectListItem
                        {
                            Text = $"{c.Name} ({OrganisationType.GovernmentBody.GetDescription()})",
                            Value = $"{c.CustomerOrganisationId.ToString()}_{OrganisationType.GovernmentBody}",
                        }).Union(_dbContext.Brokers.OrderBy(c => c.Name)
                        .Select(b => new SelectListItem
                        {
                            Text = $"{b.Name} ({OrganisationType.Broker.GetDescription()})",
                            Value = $"{b.BrokerId.ToString()}_{OrganisationType.Broker }",
                        }).Union(new SelectListItem
                        {
                            Text = "Kammarkollegiet",
                            Value = $"0_{OrganisationType.Owner }"
                        }.WrapInEnumerable()
                        ))
                        .ToList().AsReadOnly();

                    _cache.Set(organisationsSelectListKey, items, DateTimeOffset.Now.AddMinutes(15));
                }

                return items;
            }
        }

        public IEnumerable<SelectListItem> SubOrganisations(int parentOrganisationId)
        {
            return _dbContext.CustomerOrganisations
                .Where(co => co.CustomerOrganisationId == parentOrganisationId || co.ParentCustomerOrganisationId == parentOrganisationId)
                .OrderBy(c => c.ParentCustomerOrganisationId).ThenBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.CustomerOrganisationId.ToString(),
                    })
                .ToList().AsReadOnly();
        }

        public IEnumerable<SelectListItem> CustomerOrganisations
        {
            get
            {
                if (!_cache.TryGetValue(customersSelectListKey, out IEnumerable<SelectListItem> items))
                {
                    items = _dbContext.CustomerOrganisations.OrderBy(c => c.Name)
                        .Select(c => new SelectListItem
                        {
                            Text = c.Name,
                            Value = c.CustomerOrganisationId.ToString(),
                        })
                    .ToList().AsReadOnly();

                    _cache.Set(customersSelectListKey, items, DateTimeOffset.Now.AddMinutes(15));
                }

                return items;
            }
        }

        public IEnumerable<SelectListItem> ImpersonationList
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                var impersonatedUserId = !string.IsNullOrEmpty(currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)) ? currentUser.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                yield return new SelectListItem()
                {
                    Text = currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserName) ?? $"{currentUser.FindFirstValue(TolkClaimTypes.PersonalName)} (Inloggad)",
                    Value = currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserId) ?? currentUser.FindFirstValue(ClaimTypes.NameIdentifier),
                    Selected = impersonatedUserId == null
                };
                if (!_cache.TryGetValue(impersonationTargets, out IEnumerable<SelectListItem> items))
                {
                    var adminRoleId = _dbContext.Roles.Single(r => r.Name == Roles.Admin).Id;

                    items = _dbContext.Users
                        .Where(u => u.IsActive && !u.IsApiUser && !u.Roles.Select(r => r.RoleId).Contains(adminRoleId))
                        .Select(u => new SelectListItem
                        {
                            Text = !string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FullName} ({u.CustomerOrganisation.Name ?? u.Broker.Name ?? (u.InterpreterId != null ? "Tolk" : "N/A")})" : u.UserName,
                            Value = u.Id.ToString(),
                            Selected = impersonatedUserId == u.Id.ToString(),
                        }).ToList();
                }
                foreach (var item in items)
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<SelectListItem> OtherContactPersons
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                return _dbContext.Users
                    .Where(u => u.Id != currentUser.GetUserId() &&
                        u.CustomerOrganisationId == currentUser.TryGetCustomerOrganisationId())
                    .Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.UserName,
                        Value = u.Id.ToString(),
                    }).ToList();
            }
        }

        public IEnumerable<SelectListItem> CustomerUsers
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                return _dbContext.Users
                    .Where(u => u.CustomerOrganisationId == currentUser.TryGetCustomerOrganisationId())
                    .Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.UserName,
                        Value = u.Id.ToString(),
                    }).ToList();
            }
        }

        public IEnumerable<SelectListItem> BrokerUsers
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                var brokerId = currentUser.TryGetBrokerId();
                return _dbContext.Users
                    .Where(u => u.BrokerId == brokerId)
                    .Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.UserName,
                        Value = u.Id.ToString(),
                    }).ToList();
            }
        }

        public IEnumerable<SelectListItem> BrokerUsersAndInterpreters
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                var brokerId = currentUser.TryGetBrokerId();
                return _dbContext.Users
                    .Where(u => u.BrokerId == brokerId)
                    .Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FullName} (Handläggare)" : $"{u.UserName} (Handläggare)",
                        Value = u.Id.ToString(),
                    }).Union(_dbContext.Users.Where(u => u.Interpreter.Brokers.Any(b => b.BrokerId == brokerId))
                        .Select(u => new SelectListItem
                        {
                            Text = !string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FullName}" : $"{u.UserName} (Tolk)",
                            Value = u.Id.ToString(),
                        })
                    ).ToList();
            }
        }

        public const int NewInterpreterId = -1;

        public IEnumerable<SelectListItem> GetInterpreters(int brokerId, int? interpreterToBeReplacedId = null)
        {
            yield return new SelectListItem
            {
                Value = NewInterpreterId.ToString(),
                Text = "Ny tolk"
            };

            var interpretersInDb = _dbContext.InterpreterBrokers.Where(i => i.BrokerId == brokerId && i.InterpreterBrokerId != interpreterToBeReplacedId)
            .Select(i => new SelectListItem
            {
                Value = i.InterpreterBrokerId.ToString(),
                Text = string.IsNullOrWhiteSpace(i.OfficialInterpreterId) ? $"{i.FullName} (Tolk-ID: saknas)" : $"{i.FullName} (Tolk-ID: {i.OfficialInterpreterId})",
            });

            foreach (var i in interpretersInDb)
            {
                yield return i;
            }
        }

        public IEnumerable<SelectListItem> UserStatuses
        { get; } =
            EnumHelper.GetAllDescriptions<UserStatus>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public IEnumerable<SelectListItem> NotificationTypes
        { get; } =
            EnumHelper.GetAllDescriptions<NotificationType>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public IEnumerable<SelectListItem> WebhookStatuses
        { get; } =
            EnumHelper.GetAllDescriptions<WebhookStatus>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

    }
}
