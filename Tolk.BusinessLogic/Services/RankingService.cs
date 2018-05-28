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

        public IQueryable<Ranking> GetActiveRankingsForRegion(int regionId, DateTime utcNow)
        {
            //TODO: Does not handle if the order is far in the future. If it is it should take the one with the latest last valid date
            return _tolkDbContext.Rankings
                .Where(r => r.RegionId == regionId && r.FirstValidDate <= utcNow && r.LastValidDate > utcNow);
        }
    }
}
