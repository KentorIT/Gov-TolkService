using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Helpers;
using System.Data;

namespace Tolk.BusinessLogic.Services
{
    public class OrderService
    {
        private TolkDbContext _tolkDbContext;
        private ISystemClock _clock;
        private RankingService _rankingService;
        private DateCalculationService _dateCalculationService;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISystemClock clock,
            RankingService rankingService,
            DateCalculationService dateCalculationService)
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
            _dateCalculationService = dateCalculationService;
        }
        
        public void HandleExpiredRequests()
        {
            Request expiredRequest;
            do
            {
                // Pick one item at a time, and make each of them an own transaction.
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    expiredRequest = _tolkDbContext.Requests
                        .Include(r => r.Ranking)
                        .Include(r => r.Order)
                        .Where(r => r.ExpiresAt <= _clock.UtcNow && r.Status == RequestStatus.Created)
                        .FirstOrDefault();

                    if (expiredRequest != null)
                    {
                        expiredRequest.Status = RequestStatus.DeniedByTimeLimit;

                        CreateRequest(expiredRequest.Order);

                        _tolkDbContext.SaveChanges();
                        trn.Commit();
                    }
                }
            } while (expiredRequest != null);
        }

        public void CreateRequest(Order order)
        {
            var rankings = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartDateTime.Date);
            var newExpiry = CalculateExpiryForNewRequest(order.StartDateTime);

            order.CreateRequest(rankings, newExpiry);
        }

        public DateTimeOffset CalculateExpiryForNewRequest(DateTimeOffset startDate)
        {
            // Grab utcNow to not risk it flipping over during execution of the method.
            var utcNow = _clock.UtcNow;

            var daysInAdvance = _dateCalculationService.GetWorkDaysBetween(utcNow.Date, startDate.Date);

            if (daysInAdvance >= 2)
            {
                return utcNow.Date.AddDays(1).AddHours(15).ToDateTimeOffsetSweden();
            }
            if(daysInAdvance == 1 && utcNow.ToDateTimeOffsetSweden().Hour < 14)
            {
                return _dateCalculationService.GetFirstWorkDay(utcNow.Date).Add(new TimeSpan(16, 30, 0)).ToDateTimeOffsetSweden();
            }

            // TODO Need to get/understand rules for late day before or same day requests. For now a dummy
            // 1 hour response time.
            return utcNow.AddHours(1).ToDateTimeOffsetSweden();
        }

    }
}
