using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        public static IEnumerable<SelectListItem> SearchableRoles => GetList<UserType>().Where(s => s.Value != UserType.LocalAdministrator.ToString());

        public static IEnumerable<SelectListItem> SearchableRolesForCustomers =>
            GetList(new List<UserType>() { UserType.OrderCreator, UserType.OrganisationAdministrator });

        public static IEnumerable<SelectListItem> SearchableRolesForBrokers =>
            GetList(new List<UserType>() { UserType.Broker, UserType.OrganisationAdministrator });

        public static IEnumerable<SelectListItem> ComplaintStatuses => GetList<ComplaintStatus>();

        public static IEnumerable<SelectListItem> RequestStatuses { get; } =
            EnumHelper.GetAllDescriptions<RequestStatus>()
                .Where(e => e.Value != RequestStatus.AwaitingDeadlineFromCustomer
                    && e.Value != RequestStatus.NoDeadlineFromCustomer)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> ComplaintTypes => GetList<ComplaintType>();

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
        public static IEnumerable<SelectListItem> PriceListTypes => GetList<PriceListType>();

        public static IEnumerable<SelectListItem> AssignmentStatuses => GetList<AssignmentStatus>();

        public static IEnumerable<SelectListItem> AssignmentTypes => GetList<AssignmentType>();

        public static IEnumerable<SelectListItem> SystemMessageTypes => GetList<SystemMessageType>();

        public static IEnumerable<SelectListItem> SystemMessageUserTypeGroups => GetList<SystemMessageUserTypeGroup>();

        public static IEnumerable<SelectListItem> Genders => GetList<Gender>();

        public static IEnumerable<SelectListItem> BoolList => GetList<TrueFalse>();

        public static IEnumerable<SelectListItem> AllowExceedingTravelCost => GetList<AllowExceedingTravelCost>();

        public static IEnumerable<SelectListItem> InterpreterLocations => GetList<InterpreterLocation>();

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

        public static IEnumerable<SelectListItem> TaxCards => GetList<TaxCard>();

        public static IEnumerable<SelectListItem> DesireTypes { get; } =
            EnumHelper.GetAllDescriptions<DesireType>()
                .OrderByDescending(e => e.Value).Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<ExtendedSelectListItem> ReportList(UserType userType)
        {
            List<ExtendedSelectListItem> reports = new List<ExtendedSelectListItem>();

            switch (userType)
            {
                case UserType.OrderCreator:
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.OrdersForCustomer.ToString(),
                        Text = ReportType.OrdersForCustomer.GetDescription(),
                        AdditionalDataAttribute = ReportType.OrdersForCustomer.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.DeliveredOrdersCustomer.ToString(),
                        Text = ReportType.DeliveredOrdersCustomer.GetDescription(),
                        AdditionalDataAttribute = ReportType.DeliveredOrdersCustomer.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.RequisitionsForCustomer.ToString(),
                        Text = ReportType.RequisitionsForCustomer.GetDescription(),
                        AdditionalDataAttribute = ReportType.RequisitionsForCustomer.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.ComplaintsForCustomer.ToString(),
                        Text = ReportType.ComplaintsForCustomer.GetDescription(),
                        AdditionalDataAttribute = ReportType.ComplaintsForCustomer.GetCustomName()
                    });
                    break;
                case UserType.Broker:
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.RequestsForBrokers.ToString(),
                        Text = ReportType.RequestsForBrokers.GetDescription(),
                        AdditionalDataAttribute = ReportType.RequestsForBrokers.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.DeliveredOrdersBrokers.ToString(),
                        Text = ReportType.DeliveredOrdersBrokers.GetDescription(),
                        AdditionalDataAttribute = ReportType.DeliveredOrdersBrokers.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.RequisitionsForBroker.ToString(),
                        Text = ReportType.RequisitionsForBroker.GetDescription(),
                        AdditionalDataAttribute = ReportType.RequisitionsForBroker.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.ComplaintsForBroker.ToString(),
                        Text = ReportType.ComplaintsForBroker.GetDescription(),
                        AdditionalDataAttribute = ReportType.ComplaintsForBroker.GetCustomName()
                    });
                    break;
                case UserType.SystemAdministrator:
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.OrdersForSystemAdministrator.ToString(),
                        Text = ReportType.OrdersForSystemAdministrator.GetDescription(),
                        AdditionalDataAttribute = ReportType.OrdersForSystemAdministrator.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.DeliveredOrdersSystemAdministrator.ToString(),
                        Text = ReportType.DeliveredOrdersSystemAdministrator.GetDescription(),
                        AdditionalDataAttribute = ReportType.DeliveredOrdersSystemAdministrator.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.RequisitionsForSystemAdministrator.ToString(),
                        Text = ReportType.RequisitionsForSystemAdministrator.GetDescription(),
                        AdditionalDataAttribute = ReportType.RequisitionsForSystemAdministrator.GetCustomName()
                    });
                    reports.Add(new ExtendedSelectListItem
                    {
                        Value = ReportType.ComplaintsForSystemAdministrator.ToString(),
                        Text = ReportType.ComplaintsForSystemAdministrator.GetDescription(),
                        AdditionalDataAttribute = ReportType.ComplaintsForSystemAdministrator.GetCustomName()
                    });
                    break;
            }
            return reports;
        }

        public IEnumerable<SelectListItem> Languages
        {
            get
            {
                if (!_cache.TryGetValue(languagesSelectListKey, out IEnumerable<SelectListItem> items))
                {
                    items = _dbContext.Languages.Where(l => l.Active)
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

        public IEnumerable<SelectListItem> ActiveCustomerUnitsForUser
        {
            get
            {
                var customerUnitsIds = _httpContextAccessor.HttpContext.User.TryGetAllCustomerUnits();
                if (customerUnitsIds.Any())
                {
                    var items = _dbContext.CustomerUnits.Where(cu => cu.IsActive && customerUnitsIds.Contains(cu.CustomerUnitId))
                        .OrderBy(cu => cu.Name).Select(cu => new SelectListItem
                        {
                            Value = cu.CustomerUnitId.ToString(),
                            Text = cu.Name
                        }).ToList();
                    if (items.Any())
                    {
                        items[0].Selected = items.Count == 1;
                        items.Add(new SelectListItem
                        {
                            Value = "0",
                            Text = "Koppla inte till någon enhet"
                        });
                    }
                    return items.AsReadOnly();
                }
                return null;
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
        public IEnumerable<SelectListItem> ParentOrganisations
        {
            get
            {
                return _dbContext.CustomerOrganisations
                    .Where(co => co.SubCustomerOrganisations.Any())
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.CustomerOrganisationId.ToString(),
                    })
                .ToList().AsReadOnly();
            }
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
                    var adminRoleId = _dbContext.Roles.Single(r => r.Name == Roles.SystemAdministrator).Id;

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

        public IEnumerable<SelectListItem> CustomerUsersNotForCurrentUnit(int customerUnitId)
        {
            var currentUser = _httpContextAccessor.HttpContext.User;
            return _dbContext.Users
                .Where(u => u.CustomerOrganisationId == currentUser.TryGetCustomerOrganisationId()
                && !u.CustomerUnits.Any(cu => cu.CustomerUnitId == customerUnitId))
                .Select(u => new SelectListItem
                {
                    Text = $"{u.FullName} ({u.Email})",
                    Value = u.Id.ToString(),
                }).OrderBy(e => e.Text).ToList();
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

            var interpretersInDb = _dbContext.InterpreterBrokers.Where(i => i.BrokerId == brokerId && i.InterpreterBrokerId != interpreterToBeReplacedId && i.IsActive)
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

        public IEnumerable<SelectListItem> ActiveStatuses => GetList<ActiveStatus>().OrderBy(li => li.Text);

        public static IEnumerable<SelectListItem> NotificationTypes => GetList<NotificationType>();

        public static IEnumerable<SelectListItem> WebhookStatuses => GetList<WebhookStatus>();

        private static IEnumerable<SelectListItem> GetList<T>(IEnumerable<T> filterValues = null)
        {
            return EnumHelper.GetAllDescriptions(filterValues)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();
        }
    }
}
