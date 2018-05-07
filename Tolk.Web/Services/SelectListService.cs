using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Services
{
    public class SelectListService
    {
        private readonly IMemoryCache _cache;
        private readonly TolkDbContext _dbContext;

        public SelectListService(IMemoryCache cache, TolkDbContext dbContext)
        {
            _cache = cache;
            _dbContext = dbContext;
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
                new SelectListItem { Value = "", Text = "Välj..." },
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
                new SelectListItem { Value = "", Text = "Välj..." },
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
    }
}
