using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tolk.BusinessLogic.Services
{
    public class OrderService
    {
        private TolkDbContext _tolkDbContext;
        private ISystemClock _clock;
        private RankingService _rankingService;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISystemClock clock,
            RankingService rankingService)
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
        }
        
        public void HandleExpiredRequests()
        {
            var expiredRequests = _tolkDbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .Where(r => r.ExpiresAt <= _clock.UtcNow && r.Status == RequestStatus.Created);

            foreach(var r in expiredRequests)
            {
                var rankings = _rankingService.GetActiveRankingsForRegion(r.Order.RegionId, r.Order.StartDateTime.Date);

                r.Order.CreateRequest(rankings);
            }
        }
    }
}
