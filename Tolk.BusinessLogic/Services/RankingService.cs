using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Services
{
    public class RankingService
    {
        private TolkDbContext _tolkDbContext;

        public RankingService(TolkDbContext tolkDbContext)
        {
            _tolkDbContext = tolkDbContext;
        }

        public IQueryable<Ranking> GetActiveRankingsForRegion(int regionId)
        {
            var utcNow = DateTime.UtcNow;

            return _tolkDbContext.Rankings
                .Where(r => r.RegionId == regionId && r.FirstValidDate <= utcNow && r.LastValidDate > utcNow);
        }
    }
}
