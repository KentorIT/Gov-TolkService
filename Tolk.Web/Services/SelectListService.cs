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
                Value = r.Id.ToString(),
                Text = r.Name
            })
            .ToList().AsReadOnly();


        private const string languagesSelectListKey = nameof(languagesSelectListKey);

        public  IEnumerable<SelectListItem> Languages
        {
            get
            {
                IEnumerable<SelectListItem> items;
                if(!_cache.TryGetValue(languagesSelectListKey, out items))
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
