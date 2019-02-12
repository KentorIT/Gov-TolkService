﻿using Microsoft.EntityFrameworkCore;
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
        private readonly NotificationService _notificationService;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            RankingService rankingService,
            DateCalculationService dateCalculationService,
            PriceCalculationService priceCalculationService,
            ILogger<OrderService> logger,
            IOptions<TolkOptions> options,
            NotificationService notificationService
            )
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
            _dateCalculationService = dateCalculationService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _options = options.Value;
            _notificationService = notificationService;
        }

        public async Task HandleExpiredRequests()
        {
            var expiredRequestIds = await _tolkDbContext.Requests
                .Where(r => (r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received))
                    || (r.Order.StartAt <= _clock.SwedenNow && r.Status == RequestStatus.AwaitingDeadlineFromCustomer))
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
                            .SingleOrDefaultAsync(r =>
                                ((r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received))
                                || (r.Order.StartAt <= _clock.SwedenNow && r.Status == RequestStatus.AwaitingDeadlineFromCustomer))
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

                            if (expiredRequest.Order.StartAt <= _clock.SwedenNow)
                            {
                                expiredRequest.Status = RequestStatus.NoDeadlineFromCustomer;
                                await TerminateOrder(expiredRequest.Order);
                            }
                            else
                            {
                                await CreateRequest(expiredRequest.Order, expiredRequest);
                            }

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
                .Where(r => r.Order.Status == OrderStatus.RequestRespondedNewInterpreter && r.Order.StartAt.AddHours(-_options.HoursToApproveChangeInterpreterRequests) <= _clock.SwedenNow)
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

                            _notificationService.RequestChangedInterpreterAccepted(request, InterpereterChangeAcceptOrigin.SystemRule);

                            _tolkDbContext.SaveChanges();
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing {methodName} {requestId}", nameof(HandleExpiredReplacedInterpreterRequests), requestId);
                    }
                }
            }
        }

        public async Task HandleExpiredNonAnsweredRespondedRequests()
        {
            var nonAnsweredRespondedRequestsId = await _tolkDbContext.Requests
                .Where(r => (r.Order.Status == OrderStatus.RequestResponded || r.Order.Status == OrderStatus.RequestRespondedNewInterpreter)
                && r.Order.StartAt <= _clock.SwedenNow)
                .Where(r => r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                .Select(r => r.RequestId).ToListAsync();

            _logger.LogDebug("Found {count} non answered responded requests that expires: {requestIds}",
                nonAnsweredRespondedRequestsId.Count, string.Join(", ", nonAnsweredRespondedRequestsId));

            foreach (var requestId in nonAnsweredRespondedRequestsId)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var request = await _tolkDbContext.Requests
                        .Include(r => r.Order)
                        .SingleOrDefaultAsync(r => r.Order.StartAt <= _clock.SwedenNow
                        && (r.Order.Status == OrderStatus.RequestResponded || r.Order.Status == OrderStatus.RequestRespondedNewInterpreter)
                        && (r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                        && r.RequestId == requestId);
                        if (request == null)
                        {
                            _logger.LogDebug("Non answered responded request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                requestId);
                        }
                        else
                        {
                            _logger.LogInformation("Set new status for non answered responded request {requestId}.",
                                requestId);
                            request.Status = RequestStatus.ResponseNotAnsweredByCreator;
                            request.Order.Status = OrderStatus.ResponseNotAnsweredByCreator;
                            _tolkDbContext.SaveChanges();
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing {methodName} for request {requestId}", nameof(HandleExpiredNonAnsweredRespondedRequests), requestId);
                    }
                }
            }
        }

        public async Task CreateRequest(Order order, Request expiredRequest = null, DateTimeOffset? latestAnswerBy = null)
        {
            Request request = null;
            DateTimeOffset? expiry = latestAnswerBy ?? CalculateExpiryForNewRequest(order.StartAt);
            var rankings = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date);//ska vi ha med offset time här?

            if (expiredRequest != null)
            {
                // Check if expired request was created before assignment after 14:00
                if (!expiredRequest.IsTerminalRequest)
                {
                    request = order.CreateRequest(rankings, expiry, _clock.SwedenNow);
                }
            }
            else
            {
                request = order.CreateRequest(rankings, expiry, _clock.SwedenNow, latestAnswerBy.HasValue);
                //This is the first time a request is created on this order, add the priceinformation too...
                await _tolkDbContext.SaveChangesAsync();
                CreatePriceInformation(order);
            }

            // Save to get ids for the log message.
            await _tolkDbContext.SaveChangesAsync();

            if (request != null)
            {
                var newRequest = _tolkDbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order).ThenInclude(o => o.Requirements)
                    .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceCalculationCharge)
                    .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                    .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.Ranking.Broker)
                    .Single(r => r.RequestId == request.RequestId);
                if (expiry.HasValue)
                {
                    _logger.LogInformation("Created request {requestId} for order {orderId} to {brokerId} with expiry {expiry}",
                        request.RequestId, request.OrderId, request.Ranking.BrokerId, request.ExpiresAt);
                    _notificationService.RequestCreated(newRequest);
                }
                else
                {
                    // Request expiry information from customer
                    order.Status = OrderStatus.AwaitingDeadlineFromCustomer;
                    request.Status = RequestStatus.AwaitingDeadlineFromCustomer;
                    request.IsTerminalRequest = true;

                    await _tolkDbContext.SaveChangesAsync();

                    _logger.LogInformation($"Created request {request.RequestId} for order {request.OrderId}, but system was unable to calculate expiry.");
                    _notificationService.RequestCreatedWithoutExpiry(newRequest);
                }
            }
            else
            {
                await TerminateOrder(order);
            }
        }

        public async Task TerminateOrder(Order order)
        {
            if (order.Status == OrderStatus.AwaitingDeadlineFromCustomer)
            {
                order.Status = OrderStatus.NoDeadlineFromCustomer;
            }
            else
            {
                order.Status = OrderStatus.NoBrokerAcceptedOrder;
            }

            //There are no more brokers to ask.
            // Send an email to tell the order creator, and possibly the other user as well...
            var terminatedOrder = await _tolkDbContext.Orders
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .SingleAsync(o => o.OrderId == order.OrderId);
            _notificationService.OrderNoBrokerAccepted(terminatedOrder);
            _logger.LogInformation("Could not create another request for order {orderId}, no more available brokers or too close in time.",
                order.OrderId);
        }

        public async Task SetRequestExpiryManually(Request request, DateTimeOffset expiry)
        {
            request.ExpiresAt = expiry;
            request.Order.Status = OrderStatus.Requested;
            request.Status = RequestStatus.Created;

            await _tolkDbContext.SaveChangesAsync();

            // Log and notify
            _logger.LogInformation($"Expiry {expiry} manually set on request {request.RequestId}");
            _notificationService.RequestCreated(request);
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
                    r.Status == RequestStatus.Approved).RankingId
                );
            order.PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, OrderPriceRow>(row)));
            _tolkDbContext.SaveChanges();
        }

        public DisplayPriceInformation GetOrderPriceinformationForConfirmation(Order order, PriceListType pl)
        {
            CompetenceLevel cl = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(SelectCompetenceLevelForPriceEstimation(order.CompetenceRequirements?.Select(item => item.CompetenceLevel)));
            int rankingId = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date).OrderBy(r => r.Rank).FirstOrDefault().RankingId;
            return _priceCalculationService.GetPriceInformationToDisplay(_priceCalculationService.GetPrices(order.StartAt, order.EndAt, cl, pl, rankingId).PriceRows);
        }

        /// <summary>
        /// Takes an orders start time and calculates an expiry time for answering an order request.
        /// 
        /// When the appointment starts in too short of a time-frame, the system will not calculate an expiry time and return null. 
        /// When that happens: the user should manually set an expiry time.
        /// </summary>
        /// <param name="startDateTime">Time and date when the appointment starts</param>
        /// <returns>Null if startDateTime is too close in time to automatically calculate an expiry time</returns>
        public DateTimeOffset? CalculateExpiryForNewRequest(DateTimeOffset startDateTime)
        {
            // Grab current time to not risk it flipping over during execution of the method.
            var swedenNow = _clock.SwedenNow;

            if (swedenNow.Date < startDateTime.Date)
            {
                var daysInAdvance = _dateCalculationService.GetWorkDaysBetween(swedenNow.Date, startDateTime.Date);

                //if swedenNow is not a workday (calculate that the request "arrives" on next workday)
                var requestArriveDate = _dateCalculationService.GetFirstWorkDay(swedenNow.Date);

                if (daysInAdvance >= 2)
                {
                    return _dateCalculationService.GetFirstWorkDay(requestArriveDate.AddDays(1)).AddHours(15).ToDateTimeOffsetSweden();
                }
                if (daysInAdvance == 1 && swedenNow.Hour < 14)
                {
                    return requestArriveDate.Add(new TimeSpan(16, 30, 0)).ToDateTimeOffsetSweden();
                }
            }

            // Starts too soon to automatically calculate response expiry time. Customer must define a dead-line manually.
            return null;
        }

        // This is an auxilary method for calculating initial estimate
        public static CompetenceAndSpecialistLevel SelectCompetenceLevelForPriceEstimation(IEnumerable<CompetenceAndSpecialistLevel> list)
        {
            if (list == null || list.Count() == 0)
            {
                // If no level is specified, AuthorizedInterpreter should be returned
                return CompetenceAndSpecialistLevel.AuthorizedInterpreter;
            }
            if (list.Count() == 1)
            {
                return list.First();
            }
            // Otherwise, base estimation on the highest (and most expensive) competence level
            return list.OrderByDescending(item => (int)item).First();
        }

        public async Task CleanTempAttachments()
        {
            using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    _logger.LogInformation("Cleaning temporary attachments");
                    // Find attachmentgroups older than 24 hours
                    var attachmentsGroupsToDelete = _tolkDbContext.TemporaryAttachmentGroups.Where(ta => ta.CreatedAt < _clock.SwedenNow.AddDays(-1)).ToList();
                    if (attachmentsGroupsToDelete.Any())
                    {
                        _logger.LogInformation("Cleaning {0} attachmentgroups", attachmentsGroupsToDelete.Count());
                        _tolkDbContext.TemporaryAttachmentGroups.RemoveRange(attachmentsGroupsToDelete);
                        await _tolkDbContext.SaveChangesAsync();
                    }

                    // Find orphaned attachments
                    var attachmentsToDelete = _tolkDbContext.Attachments.Where(a => !_tolkDbContext.TemporaryAttachmentGroups.Select(ta => ta.AttachmentId).Contains(a.AttachmentId))
                                                                        .Where(a => !_tolkDbContext.OrderAttachments.Select(oa => oa.AttachmentId).Contains(a.AttachmentId))
                                                                        .Where(a => !_tolkDbContext.RequestAttachments.Select(ra => ra.AttachmentId).Contains(a.AttachmentId))
                                                                        .Where(a => !_tolkDbContext.RequisitionAttachments.Select(ra => ra.AttachmentId).Contains(a.AttachmentId)).ToList();
                    if (attachmentsToDelete.Any())
                    {
                        _logger.LogInformation("Cleaning {0} attachments", attachmentsToDelete.Count());
                        _tolkDbContext.Attachments.RemoveRange(attachmentsToDelete);
                        await _tolkDbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation("Done cleaning temporary attachments");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing {methodName}", nameof(CleanTempAttachments));
                }
                trn.Commit();
            }
        }
    }
}
