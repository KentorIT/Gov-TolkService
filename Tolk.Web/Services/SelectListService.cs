using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;

namespace Tolk.Web.Services
{
    public class SelectListService
    {
        private readonly IMemoryCache _cache;
        private readonly TolkDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private const string languagesSelectListKey = nameof(languagesSelectListKey);
        private const string impersonationTargets = nameof(impersonationTargets);

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
            Region.Regions.Select(r => new SelectListItem
            {
                Value = r.RegionId.ToString(),
                Text = r.Name
            })
            .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> AssignentTypes { get; } =
            EnumHelper.GetAllDescriptions<AssignmentType>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public static IEnumerable<SelectListItem> CompetenceLevels { get; } =
            EnumHelper.GetAllDescriptions<CompetenceAndSpecialistLevel>()
                .Select(e => new SelectListItem() { Text = e.Description, Value = e.Value.ToString() })
                .ToList().AsReadOnly();

        public IEnumerable<SelectListItem> Languages
        {
            get
            {
                if (!_cache.TryGetValue(languagesSelectListKey, out IEnumerable<SelectListItem> items))
                {
                    items = _dbContext.Languages
                        .OrderBy(l => l.Name).Select(l => new SelectListItem
                    {
                        Value = l.LanguageId.ToString(),
                        Text = l.Name
                    })
                    .ToList().AsReadOnly();

                    _cache.Set(languagesSelectListKey, items, DateTimeOffset.Now.AddMinutes(15));
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
                    Text = currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserName) ?? currentUser.FindFirstValue(ClaimTypes.Name),
                    Value = currentUser.FindFirstValue(TolkClaimTypes.ImpersonatingUserId) ?? currentUser.FindFirstValue(ClaimTypes.NameIdentifier),
                    Selected = impersonatedUserId == null
                };
                IEnumerable<SelectListItem> items;
                if (!_cache.TryGetValue(impersonationTargets, out items))
                {
                    var adminRoleId = _dbContext.Roles.Single(r => r.Name == Roles.Admin).Id;

                    items = _dbContext.Users
                        .Where(u => !u.Roles.Select(r => r.RoleId).Contains(adminRoleId))
                        .Select(u => new SelectListItem
                        {
                            Text = u.UserName,
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

        public IEnumerable<SelectListItem> GetInterpreters(int brokerId, int regionId)
        {
           return _dbContext.Interpreters.Where(i => i.BrokerRegions.Any(br => br.BrokerRegion.BrokerId == brokerId && br.BrokerRegion.RegionId == regionId))
           .Select(i => new SelectListItem
            {
                Value = i.InterpreterId.ToString(),
                Text = i.User.UserName
           });
        }
    }
}
