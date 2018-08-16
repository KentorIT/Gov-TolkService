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
using Microsoft.Extensions.Logging;
using Tolk.BusinessLogic.Enums;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class OrderService
    {
        private readonly TolkDbContext _tolkDbContext;
        private readonly ISwedishClock _clock;
        private readonly RankingService _rankingService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            RankingService rankingService,
            DateCalculationService dateCalculationService,
            PriceCalculationService priceCalculationService,
            ILogger<OrderService> logger)
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
            _dateCalculationService = dateCalculationService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
        }

        public async Task HandleExpiredRequests()
        {
            var expiredRequestIds = await _tolkDbContext.Requests
                .Where(r => r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received))
                .Select(r => r.RequestId)
                .ToListAsync();

            _logger.LogDebug("Found {count} expired requests to process: {requestIds}",
                expiredRequestIds.Count, string.Join(", ", expiredRequestIds));

            foreach (var requestId in expiredRequestIds)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var expiredRequest = await _tolkDbContext.Requests
                            .Include(r => r.Ranking)
                            .Include(r => r.Order)
                            .ThenInclude(o => o.Requests)
                            .ThenInclude(r => r.Ranking)
                            .SingleOrDefaultAsync(
                            r => r.ExpiresAt <= _clock.SwedenNow
                            && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)
                            && r.RequestId == requestId);

                        if (expiredRequest == null)
                        {
                            _logger.LogDebug("Request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                requestId);
                        }
                        else
                        {
                            _logger.LogInformation("Processing expired request {requestId} for Order {orderId}.",
                                expiredRequest.RequestId, expiredRequest.OrderId);

                            expiredRequest.Status = RequestStatus.DeniedByTimeLimit;

                            await CreateRequest(expiredRequest.Order);

                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing expired request {requestId}", requestId);
                    }
                }
            }
        }

        public async Task CreateRequest(Order order)
        {
            var rankings = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date);
            var newExpiry = CalculateExpiryForNewRequest(order.StartAt);

            var request = order.CreateRequest(rankings, newExpiry);

            // Save to get ids for the log message.
            await _tolkDbContext.SaveChangesAsync();

            if (request != null)
            {
                _logger.LogInformation("Created request {requestId} for order {orderId} to {brokerId} with expiry {expiry}",
                    request.RequestId, request.OrderId, request.Ranking.BrokerId, request.ExpiresAt);
                var brokerEmail = _tolkDbContext.Brokers.Single(b => b.BrokerId == request.Ranking.BrokerId).EmailAddress;
                if (!string.IsNullOrEmpty(brokerEmail))
                {
                    var createdOrder = await _tolkDbContext.Orders
                        .Include(o => o.CustomerOrganisation)
                        .Include(o => o.Region)
                        .Include(o => o.Language)
                        .SingleAsync(o => o.OrderId == request.OrderId);
                    _tolkDbContext.Add(new OutboundEmail(
                        request.Ranking.Broker.EmailAddress,
                        $"Nytt avrop registrerat: {order.OrderNumber}",
                        $"Ett nytt avrop har kommit in från {order.CustomerOrganisation.Name}.\n" +
                        $"\tRegion: {order.Region.Name}\n" +
                        $"\tSpråk: {order.OtherLanguage ?? order.Language?.Name ?? "(Tolkanvändarutbildning)"}\n" +
                        $"\tStart: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                        $"\tSlut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                        $"\tSvara senast: {request.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}\n\n" +
                        "Detta mail går inte att svara på.",
                        _clock.SwedenNow));
                }
                else
                {
                    _logger.LogInformation("No mail sent to broker {brokerId}, it has no email set.",
                       request.Ranking.BrokerId);
                }
            }
            else
            {
                //There are no more brokers to ask.
                // Send an email to tell the order creator, and possibly the other user as well...
                var terminatedOrder = await _tolkDbContext.Orders
                    .Include(o => o.CreatedByUser)
                    .SingleAsync(o => o.OrderId == order.OrderId);
                _tolkDbContext.Add(new OutboundEmail(
                    terminatedOrder.CreatedByUser.Email,
                    $"Avrop fick ingen tolk: {order.OrderNumber}",
                    $"Ingen förmedling kunde tillsätta en tolk för detta tillfälle.\n\n" +
                    "Detta mail går inte att svara på.",
                    _clock.SwedenNow));
                _logger.LogInformation("Could not create another request for order {orderId}, no more available brokers or too close in time.",
                    order.OrderId);
            }

        }

        public void CreatePriceInformation(Order order)
        {
            var priceInformation = _priceCalculationService.GetPrices(
                order.StartAt,
                order.EndAt,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(order.RequiredCompetenceLevel),
                order.CustomerOrganisation.PriceListType,
                order.Requests.Single(r =>
                    r.Status == RequestStatus.Created ||
                    r.Status == RequestStatus.Accepted ||
                    r.Status == RequestStatus.Received ||
                    r.Status == RequestStatus.Approved).Ranking.BrokerFee
                );
            foreach (var row in priceInformation.PriceRows)
            {
                order.PriceRows.Add(new OrderPriceRow
                {
                    StartAt = row.StartAt,
                    EndAt = row.EndAt,
                    IsBrokerFee = row.IsBrokerFee,
                    PriceListRowId = row.PriceListRowId,
                    TotalPrice = row.TotalPrice
                });
            }
            _tolkDbContext.SaveChanges();
        }

        public DateTimeOffset CalculateExpiryForNewRequest(DateTimeOffset startDateTime)
        {
            // Grab current time to not risk it flipping over during execution of the method.
            var swedenNow = _clock.SwedenNow;

            if (swedenNow.Date < startDateTime.Date)
            {
                var daysInAdvance = _dateCalculationService.GetWorkDaysBetween(swedenNow.Date, startDateTime.Date);

                if (daysInAdvance >= 2)
                {
                    return swedenNow.Date.AddDays(1).AddHours(15).ToDateTimeOffsetSweden();
                }
                if (daysInAdvance == 1 && swedenNow.Hour < 14)
                {
                    return _dateCalculationService.GetFirstWorkDay(swedenNow.Date).Add(new TimeSpan(16, 30, 0)).ToDateTimeOffsetSweden();
                }
            }

            // TODO Need to get/understand rules for late day before or same day requests.
            return swedenNow.AddHours(1).ToDateTimeOffsetSweden();
        }
    }
}
