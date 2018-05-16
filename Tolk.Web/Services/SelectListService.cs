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

namespace Tolk.Web.Services
{
    public class SelectListService
    {
        private readonly IMemoryCache _cache;
        private readonly TolkDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

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

        /// <summary>
        /// TODO: Should be extracted from an enum
        /// </summary>
        public static IEnumerable<SelectListItem> AssignentTypes { get; } =
            new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "På plats" },
                new SelectListItem { Value = "2", Text = "Distans" },
                new SelectListItem { Value = "3", Text = "Distans i anvisad lokal" },
                new SelectListItem { Value = "4", Text = "Tolkanvändarutbildning" },
                new SelectListItem { Value = "5", Text = "Avista" }
            }.ToList().AsReadOnly();

        /// <summary>
        /// TODO: Should be extracted from an enum
        /// RT, ST, ÖT, UT, AT
        /// </summary>
        public static IEnumerable<SelectListItem> CompetenceLevels { get; } =
            new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "RT" },
                new SelectListItem { Value = "2", Text = "ST" },
                new SelectListItem { Value = "3", Text = "AT" },
                new SelectListItem { Value = "4", Text = "UT" },
                new SelectListItem { Value = "5", Text = "ÖT" }
            }.ToList().AsReadOnly();

        private const string languagesSelectListKey = nameof(languagesSelectListKey);

        public IEnumerable<SelectListItem> Languages
        {
            get
            {
                IEnumerable<SelectListItem> items;
                if (!_cache.TryGetValue(languagesSelectListKey, out items))
                {
                    items = _dbContext.Languages.Select(l => new SelectListItem
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

        private const string impersonationTargets = nameof(impersonationTargets);

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
                    items = _dbContext.Users
                        .Where(u => !u.Roles.Select(r => r.RoleId).Contains(Roles.AdminRoleKey))
                        .Select(u => new SelectListItem
                        {
                            Text = u.UserName,
                            Value = u.Id,
                            Selected = impersonatedUserId == u.Id,
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
           return _dbContext.Users.Where(u => u.BrokerRegions.Any(br => br.BrokerRegion.BrokerId == brokerId && br.BrokerRegion.RegionId == regionId))
           .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.UserName
           });
        }
    }
}
