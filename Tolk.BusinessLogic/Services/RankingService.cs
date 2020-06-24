using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class RankingService
    {
        private readonly TolkDbContext _tolkDbContext;

        public RankingService(TolkDbContext tolkDbContext)
        {
            _tolkDbContext = tolkDbContext;
        }

        public IEnumerable<Ranking> GetActiveRankingsForRegion(int regionId, DateTime date)
        {
            if (date.TimeOfDay.Ticks != 0)
            {
                throw new ArgumentException("Date must be a pure date, without time component", nameof(date));
            }
            var rankings = _tolkDbContext.Rankings.GetActiveRankingsForRegion(regionId, date).ToList();
            var quarantines = _tolkDbContext.Quarantines.GetQuarantinesForRankings(rankings.Select(r => r.RankingId)).ToList();
            rankings.ForEach(r => r.Quarantines = quarantines.Where(q => q.RankingId == r.RankingId).ToList());
            return rankings;
        }
    }
}
