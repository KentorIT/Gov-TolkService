﻿using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Helpers;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Tolk.BusinessLogic.Services
{
    public class OrderService
    {
        private readonly TolkDbContext _tolkDbContext;
        private readonly ISwedishClock _clock;
        private readonly RankingService _rankingService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            RankingService rankingService,
            DateCalculationService dateCalculationService,
            ILoggerFactory loggerFactory)
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
            _dateCalculationService = dateCalculationService;
            _logger = loggerFactory.CreateLogger<OrderService>();
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
                        .Where(r => r.ExpiresAt <= _clock.SwedenNow && r.Status == RequestStatus.Created)
                        .FirstOrDefault();

                    if (expiredRequest != null)
                    {
                        _logger.LogInformation("Processing expired request {requestId} for Order {orderId}.",
                            expiredRequest.RequestId, expiredRequest.OrderId);

                        expiredRequest.Status = RequestStatus.DeniedByTimeLimit;

                        CreateRequest(expiredRequest.Order);

                        trn.Commit();
                    }
                }
            } while (expiredRequest != null);
        }

        public void CreateRequest(Order order)
        {
            var rankings = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartDateTime.Date);
            var newExpiry = CalculateExpiryForNewRequest(order.StartDateTime);

            var request = order.CreateRequest(rankings, newExpiry);

            if(request != null)
            {
                // Save to get ids for the log message.
                _tolkDbContext.SaveChanges();

                _logger.LogInformation("Created request {requestId} for order {orderId} to {brokerId} with expiry {expiry}",
                    request.RequestId, request.OrderId, request.Ranking.BrokerId, request.ExpiresAt);
            }
            else
            {
                _logger.LogInformation("Could not create another request for order {orderId}, no more available brokers.");
            }
        }

        public DateTimeOffset CalculateExpiryForNewRequest(DateTimeOffset startDate)
        {
            // Grab utcNow to not risk it flipping over during execution of the method.
            var swedenNow = _clock.SwedenNow;

            var daysInAdvance = _dateCalculationService.GetWorkDaysBetween(swedenNow.Date, startDate.Date);

            if (daysInAdvance >= 2)
            {
                return swedenNow.Date.AddDays(1).AddHours(15).ToDateTimeOffsetSweden();
            }
            if(daysInAdvance == 1 && swedenNow.Hour < 14)
            {
                return _dateCalculationService.GetFirstWorkDay(swedenNow.Date).Add(new TimeSpan(16, 30, 0)).ToDateTimeOffsetSweden();
            }

            // TODO Need to get/understand rules for late day before or same day requests. For now a dummy
            // 1 hour response time.
            return swedenNow.AddHours(1).ToDateTimeOffsetSweden();
        }

    }
}
