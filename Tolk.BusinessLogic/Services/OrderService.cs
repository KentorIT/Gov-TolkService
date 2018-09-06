using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
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
        private readonly TolkOptions _options;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            RankingService rankingService,
            DateCalculationService dateCalculationService,
            PriceCalculationService priceCalculationService,
            ILogger<OrderService> logger,
            IOptions<TolkOptions> options
            )
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
            _dateCalculationService = dateCalculationService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _options = options.Value;
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

        public async Task HandleExpiredComplaints()
        {
            var expiredComplaintIds = await _tolkDbContext.Complaints
                .Where(c => c.CreatedAt.AddMonths(_options.MonthsToApproveComplaints) <= _clock.SwedenNow && c.Status == ComplaintStatus.Created)
                .Select(c => c.ComplaintId)
                .ToListAsync();

            _logger.LogDebug("Found {count} expired complaints to process: {expiredComplaintIds}",
                expiredComplaintIds.Count, string.Join(", ", expiredComplaintIds));

            foreach (var complaintId in expiredComplaintIds)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var expiredComplaint = await _tolkDbContext.Complaints
                            .SingleOrDefaultAsync(c => c.CreatedAt.AddMonths(_options.MonthsToApproveComplaints) <= _clock.SwedenNow
                        && c.Status == ComplaintStatus.Created && c.ComplaintId == complaintId);

                        if (expiredComplaint == null)
                        {
                            _logger.LogDebug("Complaint {complaintId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                complaintId);
                        }
                        else
                        {
                            _logger.LogInformation("Processing expired Complaint {complaintId}.",
                                expiredComplaint.ComplaintId);

                            expiredComplaint.Status = ComplaintStatus.Confirmed;
                            expiredComplaint.AnsweredAt = _clock.SwedenNow;
                            expiredComplaint.AnswerMessage = $"Systemet har efter {_options.MonthsToApproveComplaints} månader automatiskt accepterat reklamationen då svar uteblivit.";
                            _tolkDbContext.SaveChanges();
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing expired complaint {complaintId}", complaintId);
                    }
                }
            }
        }

        public async Task HandleExpiredReplacedInterpreterRequests()
        {
            var replacedInterpreterRequestsId = await _tolkDbContext.Requests
                .Include(r => r.Order).Where(r => r.Order.Status == OrderStatus.RequestRespondedNewInterpreter && r.Order.StartAt.AddHours(-_options.HoursToApproveChangeInterpreterRequests) <= _clock.SwedenNow)
                .Where(r => r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                .Select(r => r.RequestId)
                .ToListAsync();

            _logger.LogDebug("Found {count} to be approved for replaced interpreter: {requestIds}",
                replacedInterpreterRequestsId.Count, string.Join(", ", replacedInterpreterRequestsId));

            foreach (var requestId in replacedInterpreterRequestsId)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var request = await _tolkDbContext.Requests
                            .Include(r => r.Ranking)
                            .ThenInclude(r => r.Broker)
                            .Include(r => r.Interpreter)
                            .ThenInclude(i => i.User)
                            .Include(r => r.Order)
                            .ThenInclude(o => o.Requests)
                            .Include(r => r.Order)
                            .ThenInclude(o => o.CustomerOrganisation)
                            .Include(r => r.Order)
                            .ThenInclude(o => o.CreatedByUser)
                            .SingleOrDefaultAsync(
                            r => r.Order.StartAt.AddHours(-_options.HoursToApproveChangeInterpreterRequests) <= _clock.SwedenNow && r.Order.Status == OrderStatus.RequestRespondedNewInterpreter
                            && (r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                            && r.RequestId == requestId);

                        if (request == null)
                        {
                            _logger.LogDebug("Request {requestId} was in list to be approved for replaced interpreter, but doesn't match criteria when re-read from database - skipping.",
                                requestId);
                        }
                        else
                        {
                            _logger.LogInformation("Approving replaced interpreter request {requestId} for Order {orderId}.",
                                request.RequestId, request.OrderId);

                            //TODO set a system user that is AnswerProcessedBy, probably needed for info and for display in system 
                            request.Status = RequestStatus.Approved;
                            request.AnswerProcessedAt = _clock.SwedenNow;
                            request.Order.Status = OrderStatus.ResponseAccepted;

                            _tolkDbContext.Add(new OutboundEmail(
                            request.Interpreter.User.Email,
                            $"Tilldelat tolkuppdrag avrops-ID {request.Order.OrderNumber}",
                            $"Du har fått ett tolkuppdrag hos {request.Order.CustomerOrganisation.Name} från förmedling {request.Ranking.Broker.Name}. Uppdraget har avrops-ID {request.Order.OrderNumber} och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.\n\nDetta mejl går inte att svara på.",
                            _clock.SwedenNow));

                            _tolkDbContext.Add(new OutboundEmail(
                            request.Order.CreatedByUser.Email,
                            $"Svar på avrop med avrops-ID {request.Order.OrderNumber} har godkänts av systemet",
                            $"Svar på avrop {request.Order.OrderNumber} där tolk har bytts ut har godkänts av systemet då uppdraget startar inom {_options.HoursToApproveChangeInterpreterRequests} timmar. Uppdraget startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.\n\nDetta mejl går inte att svara på.",
                            _clock.SwedenNow));

                            _tolkDbContext.SaveChanges();
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing ApproveReplacedInterpreter {requestId}", requestId);
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
            _logger.LogInformation("Create price rows for Order: {orderId}, Customer: {Name}",
                order?.OrderId, order?.CustomerOrganisation?.Name);
            var priceInformation = _priceCalculationService.GetPrices(
                order.StartAt,
                order.EndAt,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(SelectCompetenceLevelForPriceEstimation(order.CompetenceRequirements?.Select(item => item.CompetenceLevel))),
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

        // This is an auxilary method for calculating initial estimate
        public static CompetenceAndSpecialistLevel SelectCompetenceLevelForPriceEstimation(IEnumerable<CompetenceAndSpecialistLevel> list)
        {
            if (list == null || list.Count() == 0)
            {
                // Choose the lowest if no level is specified
                return CompetenceAndSpecialistLevel.OtherInterpreter;
            }
            if (list.Count() == 1)
            {
                return list.First();
            }
            // Otherwise, base estimation on the highest (and most expensive) competence level
            return list.OrderByDescending(item => (int)item).First();
        }
    }
}
