using Microsoft.EntityFrameworkCore;
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
        private readonly TolkDbContext _tolkDbContext;

        public RankingService(TolkDbContext tolkDbContext)
        {
            _tolkDbContext = tolkDbContext;
        }

        public IQueryable<Ranking> GetActiveRankingsForRegion(int regionId, DateTime date)
        {
            if(date.TimeOfDay.Ticks != 0)
            {
                throw new ArgumentException("Date must be a pure date, without time component", nameof(date));
            }

            return _tolkDbContext.Rankings.Include(r => r.Quarantines)
                .Where(r => r.RegionId == regionId && r.FirstValidDate <= date && r.LastValidDate >= date);
        }
    }
}
