using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Tolk.BusinessLogic;
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
        private readonly IDistributedCache _cache;
        private readonly TolkDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string languagesSelectListKey = nameof(languagesSelectListKey);
        private const string brokersSelectListKey = nameof(brokersSelectListKey);
        private const string customersSelectListKey = nameof(customersSelectListKey);
        private const string organisationsSelectListKey = nameof(organisationsSelectListKey);
        private static readonly DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

        public SelectListService(
            IDistributedCache cache,
            TolkDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public static IEnumerable<SelectListItem> Regions =>
            Region.Regions.OrderBy(r => r.Name)
            .Select(r => new SelectListItem
            {
                Value = r.RegionId.ToSwedishString(),
                Text = r.Name
            })
            .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> SearchableRoles => GetList<UserTypes>().Where(s => s.Value != UserTypes.LocalAdministrator.ToString());

        public static IEnumerable<SelectListItem> SearchableRolesForCustomers =>
            GetList(new List<UserTypes>() { UserTypes.CentralOrderHandler, UserTypes.OrderCreator, UserTypes.OrganisationAdministrator });

        public static IEnumerable<SelectListItem> SearchableRolesForBrokers =>
            GetList(new List<UserTypes>() { UserTypes.Broker, UserTypes.OrganisationAdministrator });

        public static IEnumerable<SelectListItem> ComplaintStatuses => GetList<ComplaintStatus>();

        public static IEnumerable<SelectListItem> RequestStatuses =>
            EnumHelper.GetAllDescriptions<RequestStatus>()
                .Where(e => e.Value != RequestStatus.AwaitingDeadlineFromCustomer &&
                    e.Value != RequestStatus.NoDeadlineFromCustomer &&
                    e.Value != RequestStatus.InterpreterReplaced &&
                    e.Value != RequestStatus.PartiallyAccepted &&
                    e.Value != RequestStatus.PartiallyApproved)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> ComplaintTypes => GetList<ComplaintType>();

        public static IEnumerable<SelectListItem> RequisitionStatuses =>
            EnumHelper.GetAllDescriptions<RequisitionStatus>()
                .Where(e => e.Value != RequisitionStatus.Approved && e.Value != RequisitionStatus.DeniedByCustomer)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> OrderStatuses =>
            EnumHelper.GetAllDescriptions<OrderStatus>()
            .Where(e => e.Value != OrderStatus.DeliveryAccepted &&
                e.Value != OrderStatus.GroupAwaitingPartialResponse &&
                e.Value != OrderStatus.RequestAwaitingPartialAccept)
            .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
            .ToList().AsReadOnly();
        public static IEnumerable<SelectListItem> PriceListTypes => GetList<PriceListType>();
        public static IEnumerable<SelectListItem> TravelCostAgreementTypes => GetList<TravelCostAgreementType>();

        public static IEnumerable<SelectListItem> AssignmentStatuses => GetList<AssignmentStatus>();

        public static IEnumerable<SelectListItem> AssignmentTypes => GetList<AssignmentType>();

        public static IEnumerable<SelectListItem> SystemMessageTypes => GetList<SystemMessageType>();

        public static IEnumerable<SelectListItem> SystemMessageUserTypeGroups => GetList<SystemMessageUserTypeGroup>();

        public static IEnumerable<SelectListItem> Genders => GetList<Gender>();

        public static IEnumerable<SelectListItem> BoolList => GetList<TrueFalse>();

        public static IEnumerable<SelectListItem> AllowExceedingTravelCost => GetList<AllowExceedingTravelCost>();

        public static IEnumerable<SelectListItem> InterpreterLocations => GetList<InterpreterLocation>();

        public static IEnumerable<SelectListItem> FilteredInterpreterLocations(IEnumerable<InterpreterLocation> filter)
        {
            return EnumHelper.GetAllDescriptions<InterpreterLocation>()
                .Where(e => filter.Contains(e.Value))
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();
        }

        public static IEnumerable<SelectListItem> FilteredCompetenceLevels(IEnumerable<CompetenceAndSpecialistLevel> filter)
        {
            return EnumHelper.GetAllDescriptions<CompetenceAndSpecialistLevel>()
                .Where(e => filter.Contains(e.Value))
                .OrderByDescending(e => (int)e.Value)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();
        }

        public static IEnumerable<SelectListItem> CompetenceLevels =>
            EnumHelper.GetAllDescriptions<CompetenceAndSpecialistLevel>()
                .Where(e => e.Value > 0)
                .OrderByDescending(e => (int)e.Value)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> RequirementTypes =>
            EnumHelper.GetAllDescriptions<RequirementType>()
                .Where(e => e.Value != RequirementType.Dialect)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> TaxCards => GetList<TaxCardType>();

        public static IEnumerable<SelectListItem> DesireTypes =>
            EnumHelper.GetAllDescriptions<DesireType>()
                .OrderByDescending(e => e.Value).Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<ExtendedSelectListItem> ReportList(UserTypes userType)
        {
            List<ExtendedSelectListItem> reports = new List<ExtendedSelectListItem>();

            switch (userType)
            {
                case UserTypes.OrderCreator:
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
                case UserTypes.Broker:
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
                case UserTypes.SystemAdministrator:
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

        public IEnumerable<ExtendedSelectListItem> Languages
        {
            get
            {
                var items = _cache.Get(languagesSelectListKey).FromByteArray<IEnumerable<SerializableExtendedSelectListItem>>();
                if (items == null)
                {
                    items = _dbContext.Languages.Where(l => l.Active)
                        .OrderBy(l => l.Name).Select(l => new SerializableExtendedSelectListItem
                        {
                            Value = l.LanguageId.ToSwedishString(),
                            Text = l.Name,
                            AdditionalDataAttribute = string.IsNullOrEmpty(l.TellusName) ? string.Empty : l.Competences
                        })
                    .ToList().AsReadOnly();

                    _cache.Set(languagesSelectListKey, items.ToByteArray(), cacheOptions);
                }

                return items.GetExtendedSelectListItems();
            }
        }

        public IEnumerable<SelectListItem> ActiveCustomerUnitsForUser(bool selectOneAndOnly = true)
        {
            var customerUnitsIds = _httpContextAccessor.HttpContext.User.TryGetAllCustomerUnits();
            if (customerUnitsIds.Any())
            {
                var items = _dbContext.CustomerUnits.Where(cu => cu.IsActive && customerUnitsIds.Contains(cu.CustomerUnitId))
                    .OrderBy(cu => cu.Name).Select(cu => new SelectListItem
                    {
                        Value = cu.CustomerUnitId.ToSwedishString(),
                        Text = cu.Name
                    }).ToList();
                if (items.Any())
                {
                    items[0].Selected = selectOneAndOnly && items.Count == 1;
                    items.Add(new SelectListItem
                    {
                        Value = "0",
                        Text = Constants.SelectNoUnit
                    });
                }
                return items.AsReadOnly();
            }
            return null;
        }

        public IEnumerable<SelectListItem> Brokers
        {
            get
            {
                var items = _cache.Get(brokersSelectListKey).FromByteArray<IEnumerable<SerializableExtendedSelectListItem>>();
                if (items == null)
                {
                    items = _dbContext.Brokers.OrderBy(b => b.Name)
                        .Select(b => new SerializableExtendedSelectListItem
                        {
                            Text = b.Name,
                            Value = b.BrokerId.ToSwedishString(),
                        })
                    .ToList().AsReadOnly();

                    _cache.Set(brokersSelectListKey, items.ToByteArray(), cacheOptions);
                }

                return items.GetSelectListItems();
            }
        }

        public IEnumerable<ExtendedSelectListItem> Organisations
        {
            get
            {
                var items = _cache.Get(organisationsSelectListKey).FromByteArray<IEnumerable<SerializableExtendedSelectListItem>>();
                if (items == null)
                {
                    var list = _dbContext.CustomerOrganisations.OrderBy(c => c.Name)
                        .Select(c => new SerializableExtendedSelectListItem
                        {
                            Text = $"{c.Name} ({OrganisationType.GovernmentBody.GetDescription()})",
                            Value = $"{c.CustomerOrganisationId.ToSwedishString()}_{OrganisationType.GovernmentBody}",
                            AdditionalDataAttribute = OrganisationType.GovernmentBody.ToString(),
                        }).ToList();
                    list.AddRange(_dbContext.Brokers.OrderBy(c => c.Name)
                        .Select(b => new SerializableExtendedSelectListItem
                        {
                            Text = $"{b.Name} ({OrganisationType.Broker.GetDescription()})",
                            Value = $"{b.BrokerId.ToSwedishString()}_{OrganisationType.Broker }",
                            AdditionalDataAttribute = OrganisationType.Broker.ToString(),
                        }).ToList());
                    list.Add(new SerializableExtendedSelectListItem
                    {
                        Text = "Kammarkollegiet",
                        Value = $"0_{OrganisationType.Owner }",
                        AdditionalDataAttribute = OrganisationType.Owner.ToString(),
                    });
                    items = list.AsReadOnly();
                    _cache.Set(organisationsSelectListKey, items.ToByteArray(), cacheOptions);
                }
                return items.GetExtendedSelectListItems();
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
                        Value = c.CustomerOrganisationId.ToSwedishString(),
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
                        Value = c.CustomerOrganisationId.ToSwedishString(),
                    })
                .ToList().AsReadOnly();
            }
        }

        public IEnumerable<SelectListItem> CustomerOrganisations
        {
            get
            {
                var items = _cache.Get(customersSelectListKey).FromByteArray<IEnumerable<SerializableExtendedSelectListItem>>();
                if (items == null)
                {
                    items = _dbContext.CustomerOrganisations.OrderBy(c => c.Name)
                        .Select(c => new SerializableExtendedSelectListItem
                        {
                            Text = c.Name,
                            Value = c.CustomerOrganisationId.ToSwedishString(),
                        })
                    .ToList().AsReadOnly();

                    _cache.Set(customersSelectListKey, items.ToByteArray(), cacheOptions);
                }

                return items.GetSelectListItems();
            }
        }

        public IEnumerable<ExtendedSelectListItem> ImpersonationList
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                var impersonatedUserId = !string.IsNullOrEmpty(currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)) ? currentUser.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                yield return new ExtendedSelectListItem()
                {
                    Text = currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserName) ?? $"{currentUser.FindFirstValue(TolkClaimTypes.PersonalName)} (Inloggad)",
                    Value = currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserId) ?? currentUser.FindFirstValue(ClaimTypes.NameIdentifier),
                    Selected = impersonatedUserId == null
                };
                IEnumerable<ExtendedSelectListItem> items = _dbContext.Users
                        .Where(u => u.IsActive && !u.IsApiUser &&
                        (u.InterpreterId.HasValue || u.BrokerId.HasValue || u.CustomerOrganisationId.HasValue))
                        .Select(u => new ExtendedSelectListItem
                        {
                            Text = !string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FullName} ({u.CustomerOrganisation.Name ?? u.Broker.Name ?? (u.InterpreterId != null ? "Tolk" : "N/A")})" : u.UserName,
                            Value = u.Id.ToSwedishString(),
                            Selected = impersonatedUserId == u.Id.ToSwedishString(),
                        }).ToList();
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
                        Value = u.Id.ToSwedishString(),
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
                        Value = u.Id.ToSwedishString(),
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
                    Value = u.Id.ToSwedishString(),
                }).OrderBy(e => e.Text).ToList();
        }

        public IEnumerable<SelectListItem> CustomerUnitsForCurrentUser
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                var organisationAdmin = (currentUser.IsInRole(Roles.CentralAdministrator) || currentUser.IsInRole(Roles.CentralOrderHandler));
                var units = organisationAdmin ? _dbContext.CustomerUnits.GetCustomerUnitsForCustomerOrganisation(currentUser.TryGetCustomerOrganisationId()) :
                    _dbContext.CustomerUnits.GetCustomerUnitsForUser(currentUser.GetUserId());
                return units.OrderByDescending(cu => cu.IsActive).ThenBy(cu => cu.Name).Select(cu => new SelectListItem
                {
                    Text = $"{cu.Name} {(cu.IsActive ? string.Empty : "(Inaktiv)")}",
                    Value = cu.CustomerUnitId.ToSwedishString(),
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
                        Value = u.Id.ToSwedishString(),
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
                        Value = u.Id.ToSwedishString(),
                    }).Union(_dbContext.Users.Where(u => u.Interpreter.Brokers.Any(b => b.BrokerId == brokerId))
                        .Select(u => new SelectListItem
                        {
                            Text = !string.IsNullOrWhiteSpace(u.FullName) ? $"{u.FullName}" : $"{u.UserName} (Tolk)",
                            Value = u.Id.ToSwedishString(),
                        })
                    ).ToList();
            }
        }

        public IEnumerable<ExtendedSelectListItem> GetInterpreters(int brokerId, int? interpreterToBeReplacedId = null, int? otherInterpreterId = null, bool allowDeclineInList = false)
        {
            yield return new ExtendedSelectListItem
            {
                Value = Constants.NewInterpreterId.ToSwedishString(),
                Text = "Ny tolk",
                AdditionalDataAttribute = string.Empty
            };
            if (allowDeclineInList)
            {
                yield return new ExtendedSelectListItem
                {
                    Value = Constants.DeclineInterpreterId.ToSwedishString(),
                    Text = "Tacka nej till uppdrag",
                    AdditionalDataAttribute = string.Empty
                };
            }
            //always display protetected interpreter
            var interpretersInDb = _dbContext.InterpreterBrokers.
                Where(i => i.BrokerId == brokerId && i.IsActive &&
                ((i.InterpreterBrokerId != interpreterToBeReplacedId && i.InterpreterBrokerId != otherInterpreterId) || i.Interpreter.IsProtected))
                .Select(i => new ExtendedSelectListItem
                {
                    Value = i.InterpreterBrokerId.ToSwedishString(),
                    Text = string.IsNullOrWhiteSpace(i.OfficialInterpreterId) ? $"{i.FullName} (KamK tolknr: saknas)" : $"{i.FullName} (KamK tolknr: {i.OfficialInterpreterId})",
                    AdditionalDataAttribute = i.InterpreterId.HasValue ? i.Interpreter.IsProtected ? "Protected" : string.Empty : string.Empty
                });
            foreach (var i in interpretersInDb)
            {
                yield return i;
            }
        }

        public static IEnumerable<SelectListItem> ActiveStatuses => GetList<ActiveStatus>().OrderBy(li => li.Text);

        public static IEnumerable<SelectListItem> NotificationTypes => GetList<NotificationType>();

        public static IEnumerable<SelectListItem> WebhookStatuses => GetList<WebhookStatus>();

        public static IEnumerable<SelectListItem> DisplayForUserRoles => GetList<DisplayUserRole>();

        private static IEnumerable<SelectListItem> GetList<T>(IEnumerable<T> filterValues = null)
        {
            return EnumHelper.GetAllDescriptions(filterValues)
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();
        }
    }
}
