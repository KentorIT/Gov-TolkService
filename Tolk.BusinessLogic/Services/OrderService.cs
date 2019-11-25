using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly INotificationService _notificationService;
        private readonly VerificationService _verificationService;
        private readonly EmailService _emailService;
        private readonly ITolkBaseOptions _tolkBaseOptions;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            RankingService rankingService,
            DateCalculationService dateCalculationService,
            PriceCalculationService priceCalculationService,
            ILogger<OrderService> logger,
            INotificationService notificationService,
            VerificationService verificationService,
            EmailService emailService,
            ITolkBaseOptions tolkBaseOptions
            )
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _rankingService = rankingService;
            _dateCalculationService = dateCalculationService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _notificationService = notificationService;
            _verificationService = verificationService;
            _emailService = emailService;
            _tolkBaseOptions = tolkBaseOptions;
        }

        public async Task HandleAllScheduledTasks()
        {
            await HandleStartedOrders();
            await HandleExpiredRequests();
            await HandleExpiredRequestGroups();
            await HandleExpiredNonAnsweredRespondedRequests();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task HandleExpiredComplaints()
        {
            var expiredComplaintIds = await _tolkDbContext.Complaints
                .Where(c => c.CreatedAt.AddMonths(_tolkBaseOptions.MonthsToApproveComplaints) <= _clock.SwedenNow && c.Status == ComplaintStatus.Created)
                .Select(c => c.ComplaintId)
                .ToListAsync();

            _logger.LogInformation("Found {count} expired complaints to process: {expiredComplaintIds}",
                expiredComplaintIds.Count, string.Join(", ", expiredComplaintIds));

            foreach (var complaintId in expiredComplaintIds)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var expiredComplaint = await _tolkDbContext.Complaints
                            .SingleOrDefaultAsync(c => c.CreatedAt.AddMonths(_tolkBaseOptions.MonthsToApproveComplaints) <= _clock.SwedenNow
                        && c.Status == ComplaintStatus.Created && c.ComplaintId == complaintId);

                        if (expiredComplaint == null)
                        {
                            _logger.LogInformation("Complaint {complaintId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                complaintId);
                        }
                        else
                        {
                            _logger.LogInformation("Processing expired Complaint {complaintId}.",
                                expiredComplaint.ComplaintId);

                            expiredComplaint.Status = ComplaintStatus.AutomaticallyConfirmedDueToNoAnswer;
                            expiredComplaint.AnsweredAt = _clock.SwedenNow;
                            expiredComplaint.AnswerMessage = $"Systemet har efter {_tolkBaseOptions.MonthsToApproveComplaints} månader automatiskt godtagit reklamationen då svar uteblivit.";
                            await _tolkDbContext.SaveChangesAsync();
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing expired complaint {complaintId}", complaintId);
                        trn.Rollback();
                        await SendErrorMail(nameof(HandleExpiredComplaints), ex);
                    }
                }
            }
        }

        public async Task CreateRequestGroup(OrderGroup group, RequestGroup expiredRequestGroup = null, DateTimeOffset? latestAnswerBy = null)
        {
            NullCheckHelper.ArgumentCheckNull(group, nameof(CreateRequestGroup), nameof(OrderService));
            RequestGroup requestGroup = null;
            DateTimeOffset? expiry = latestAnswerBy ?? CalculateExpiryForNewRequest(group.ClosestStartAt);
            var rankings = _rankingService.GetActiveRankingsForRegion(group.RegionId, group.ClosestStartAt.Date);

            if (expiredRequestGroup != null)
            {
                if (!expiredRequestGroup.IsTerminalRequest)
                {
                    requestGroup = group.CreateRequestGroup(rankings, expiry, _clock.SwedenNow);
                }
            }
            else
            {
                requestGroup = group.CreateRequestGroup(rankings, expiry, _clock.SwedenNow, latestAnswerBy.HasValue);
                //This is the first time a request is created on this order, add the priceinformation too...
                await _tolkDbContext.SaveChangesAsync();
                CreatePriceInformation(group);
            }

            // Save to get ids for the log message.
            await _tolkDbContext.SaveChangesAsync();

            if (requestGroup != null)
            {
                RequestGroup newRequestGroup = await GetNewRequestGroup(requestGroup.RequestGroupId);
                if (expiry.HasValue)
                {
                    _logger.LogInformation("Created request group {requestGroupId} for order group {orderGroupId} to {brokerId} with expiry {expiry}",
                        requestGroup.RequestGroupId, requestGroup.OrderGroupId, requestGroup.Ranking.BrokerId, requestGroup.ExpiresAt);
                    _notificationService.RequestGroupCreated(newRequestGroup);
                    return;
                }
                else
                {
                    //Note: This is only valid if this is an one occasion, extra interpreter group!
                    if (group.IsSingleOccasion)
                    {
                        // Request expiry information from customer
                        group.AwaitDeadlineFromCustomer();

                        await _tolkDbContext.SaveChangesAsync();

                        _logger.LogInformation($"Created request group {requestGroup.RequestGroupId} for order group {requestGroup.OrderGroupId}, but system was unable to calculate expiry.");
                        _notificationService.RequestGroupCreatedWithoutExpiry(newRequestGroup);
                        return;
                    }
                }
            }
            await TerminateOrderGroup(group);
        }

        public async Task CreatePartialRequestGroup(RequestGroup requestGroup, IEnumerable<Request> declinedRequests)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(CreatePartialRequestGroup), nameof(OrderService));
            NullCheckHelper.ArgumentCheckNull(declinedRequests, nameof(CreatePartialRequestGroup), nameof(OrderService));

            if (requestGroup.IsTerminalRequest)
            {
                //Possibly a terminatepartial, with its own notification to customer?
                throw new NotImplementedException("Vet inte riktigt vart vi skall ta vägen om det här händer...");
            }

            DateTimeOffset? expiry = CalculateExpiryForNewRequest(declinedRequests.ClosestStartAt());
            OrderGroup group = requestGroup.OrderGroup;

            var rankings = _rankingService.GetActiveRankingsForRegion(group.RegionId, group.ClosestStartAt.Date);
            RequestGroup partialRequestGroup = group.CreatePartialRequestGroup(declinedRequests, rankings, expiry, _clock.SwedenNow);
            // Save to get ids for the log message.
            await _tolkDbContext.SaveChangesAsync();

            if (partialRequestGroup != null)
            {
                RequestGroup newRequestGroup = await GetNewRequestGroup(partialRequestGroup.RequestGroupId);
                if (expiry.HasValue)
                {
                    _logger.LogInformation("Created request group {requestGroupId} for order group {orderGroupId} to {brokerId} with expiry {expiry}",
                        partialRequestGroup.RequestGroupId, partialRequestGroup.OrderGroupId, partialRequestGroup.Ranking.BrokerId, partialRequestGroup.ExpiresAt);
                    _notificationService.RequestGroupCreated(newRequestGroup);
                    return;
                }
                else
                {
                    if (group.IsSingleOccasion)
                    {
                        // Request expiry information from customer
                        group.AwaitDeadlineFromCustomer();

                        await _tolkDbContext.SaveChangesAsync();

                        _logger.LogInformation($"Created request group {requestGroup.RequestGroupId} for order group {requestGroup.OrderGroupId}, but system was unable to calculate expiry.");
                        _notificationService.RequestGroupCreatedWithoutExpiry(newRequestGroup);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException("Vet inte riktigt vart vi skall ta vägen om det här händer...");
                    }
                }
            }
        }

        public async Task CreateRequest(Order order, Request expiredRequest = null, DateTimeOffset? latestAnswerBy = null, bool notify = true)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(CreateRequest), nameof(OrderService));
            Request request = null;
            DateTimeOffset? expiry = latestAnswerBy ?? CalculateExpiryForNewRequest(order.StartAt);
            var rankings = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date);//ska vi ha med offset time här?

            if (expiredRequest != null)
            {
                // Only create a new request if the previous request was not a flagged as terminal.
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
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
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
                    if (notify)
                    {
                        _notificationService.RequestCreated(newRequest);
                    }
                }
                else
                {
                    // Request expiry information from customer
                    order.Status = OrderStatus.AwaitingDeadlineFromCustomer;
                    request.Status = RequestStatus.AwaitingDeadlineFromCustomer;
                    request.IsTerminalRequest = true;

                    await _tolkDbContext.SaveChangesAsync();

                    _logger.LogInformation($"Created request {request.RequestId} for order {request.OrderId}, but system was unable to calculate expiry.");
                    if (notify)
                    {
                        _notificationService.RequestCreatedWithoutExpiry(newRequest);
                    }
                }
            }
            else
            {
                await TerminateOrder(order, notify);
            }
        }

        public async Task ReplaceOrder(Order order, Order replacementOrder, int userId, int? impersonatorId, string cancelMessage)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(ReplaceOrder), nameof(OrderService));
            NullCheckHelper.ArgumentCheckNull(replacementOrder, nameof(ReplaceOrder), nameof(OrderService));
            var request = order.ActiveRequest;
            if (request == null)
            {
                throw new InvalidOperationException($"Order {order.OrderId} has no active requests that can be cancelled");
            }
            var replacingRequest = new Request(request, order.StartAt, _clock.SwedenNow);
            replacementOrder.Requests.Add(replacingRequest);
            await _tolkDbContext.AddAsync(replacementOrder);
            CancelOrder(order, userId, impersonatorId, cancelMessage, true);

            replacementOrder.CreatedAt = _clock.SwedenNow;
            replacementOrder.CreatedBy = userId;
            replacementOrder.ImpersonatingCreator = impersonatorId;
            replacementOrder.Requirements = order.Requirements.Select(r => new OrderRequirement
            {
                Description = r.Description,
                IsRequired = r.IsRequired,
                RequirementType = r.RequirementType,
                RequirementAnswers = r.RequirementAnswers
                .Where(a => a.RequestId == request.RequestId)
                .Select(a => new OrderRequirementRequestAnswer
                {
                    Answer = a.Answer,
                    CanSatisfyRequirement = a.CanSatisfyRequirement,
                    RequestId = replacingRequest.RequestId
                }).ToList(),
            }).ToList();

            //Generate new price rows from current times, might be subject to change!!!
            CreatePriceInformation(replacementOrder);
            await _tolkDbContext.SaveChangesAsync();

            _notificationService.OrderReplacementCreated(order);
            _logger.LogInformation("Order {orderId} replaced by customer {userId}.", order.OrderId, userId);
        }

        public void ApproveRequestAnswer(Request request, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ApproveRequestAnswer), nameof(OrderService));
            bool isInterpreterChangeApproval = request.Status == RequestStatus.AcceptedNewInterpreterAppointed;
            request.Approve(_clock.SwedenNow, userId, impersonatorId);

            if (isInterpreterChangeApproval)
            {
                _notificationService.RequestChangedInterpreterAccepted(request, InterpereterChangeAcceptOrigin.User);
            }
            else
            {
                _notificationService.RequestAnswerApproved(request);
            }
        }

        public void ApproveRequestGroupAnswer(RequestGroup requestGroup, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(ApproveRequestGroupAnswer), nameof(OrderService));
            requestGroup.Approve(_clock.SwedenNow, userId, impersonatorId);

                _notificationService.RequestGroupAnswerApproved(requestGroup);
        }

        public async Task DenyRequestAnswer(Request request, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(DenyRequestAnswer), nameof(OrderService));
            request.Deny(_clock.SwedenNow, userId, impersonatorId, message);
            await CreateRequest(request.Order, request);
            _notificationService.RequestAnswerDenied(request);
        }

        public async Task DenyRequestGroupAnswer(RequestGroup requestGroup, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(DenyRequestGroupAnswer), nameof(OrderService));
            requestGroup.Deny(_clock.SwedenNow, userId, impersonatorId, message);
            await CreateRequestGroup(requestGroup.OrderGroup, requestGroup);
            _notificationService.RequestGroupAnswerDenied(requestGroup);
        }

        public async Task ConfirmCancellationByBroker(Request request, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmCancellationByBroker), nameof(OrderService));
            if (request.Status != RequestStatus.CancelledByBroker)
            {
                throw new InvalidOperationException($"The order {request.OrderId} has not been cancelled");
            }
            await _tolkDbContext.AddAsync(new RequestStatusConfirmation
            {
                Request = request,
                ConfirmedBy = userId,
                ImpersonatingConfirmedBy = impersonatorId,
                RequestStatus = request.Status,
                ConfirmedAt = _clock.SwedenNow
            });
        }

        public async Task ConfirmNoAnswer(Order order, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(ConfirmNoAnswer), nameof(OrderService));
            if (order.Status != OrderStatus.NoBrokerAcceptedOrder)
            {
                throw new InvalidOperationException($"The order {order.OrderId} has not been denied by all brokers");
            }
            await _tolkDbContext.AddAsync(new OrderStatusConfirmation
            {
                Order = order,
                ConfirmedBy = userId,
                ImpersonatingConfirmedBy = impersonatorId,
                OrderStatus = order.Status,
                ConfirmedAt = _clock.SwedenNow
            });
        }

        public async Task ConfirmNoAnswer(OrderGroup orderGroup, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(ConfirmNoAnswer), nameof(OrderService));
            if (orderGroup.Status != OrderStatus.NoBrokerAcceptedOrder)
            {
                throw new InvalidOperationException($"The order group {orderGroup.OrderGroupId} has not been denied by all brokers");
            }
            await _tolkDbContext.AddAsync(new OrderGroupStatusConfirmation
            {
                OrderGroup = orderGroup,
                ConfirmedBy = userId,
                ImpersonatingConfirmedBy = impersonatorId,
                OrderStatus = orderGroup.Status,
                ConfirmedAt = _clock.SwedenNow
            });
        }

        public void CancelOrder(Order order, int userId, int? impersonatorId, string message, bool isReplaced = false)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(CancelOrder), nameof(OrderService));
            var request = order.ActiveRequest;
            if (request == null)
            {
                throw new InvalidOperationException($"Order {order.OrderId} has no active requests that can be cancelled");
            }
            var now = _clock.SwedenNow;
            //If this is an approved request, and the cancellation is done to late, a requisition with full compensation will be created
            // But only if the order has not been replaced!
            bool createFullCompensationRequisition = !isReplaced && _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(now.DateTime, order.StartAt.DateTime) < 2;
            request.Cancel(now, userId, impersonatorId, message, createFullCompensationRequisition, isReplaced);

            if (!isReplaced)
            {
                //Only notify if the order was not replaced.
                _notificationService.OrderCancelledByCustomer(request, createFullCompensationRequisition);
            }
            _logger.LogInformation("Order {orderId} cancelled by customer {userId}.", order.OrderId, userId);
        }

        public void CancelOrderGroup(OrderGroup orderGroup, int userId, int? impersonatorId, string message, bool isReplaced = false)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(CancelOrderGroup), nameof(OrderService));
            var requestGroup = orderGroup.ActiveRequestGroup;
            if (requestGroup == null)
            {
                throw new InvalidOperationException($"Order group {orderGroup.OrderGroupId} has no active request group that can be cancelled");
            }
            var now = _clock.SwedenNow;
            //If this is an approved request, and the cancellation is done to late, a requisition with full compensation will be created
            // But only if the order has not been replaced!
            foreach (var order in orderGroup.Orders.Where(o => o.StartAt > _clock.SwedenNow))
                //Add filter for correct statuses, only "active" orders can be cancelled.
            {
                CancelOrder(order, userId, impersonatorId, message);
            }
            //Add code for canceling the actual order group and also the underlying (might be more than one) requestGroups.
            _logger.LogInformation("Order group {orderGroupId} cancelled by customer {userId}.", orderGroup.OrderGroupId, userId);
        }

        public void SetRequestExpiryManually(Request request, DateTimeOffset expiry, int userId, int? impersonatingUserId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(SetRequestExpiryManually), nameof(OrderService));
            if (request.Status != RequestStatus.AwaitingDeadlineFromCustomer)
            {
                throw new InvalidOperationException($"There is no request awaiting deadline from customer on this order {request.OrderId}");
            }
            request.ExpiresAt = expiry;
            request.Order.Status = OrderStatus.Requested;
            request.Status = RequestStatus.Created;
            request.RequestUpdateLatestAnswerTime = new RequestUpdateLatestAnswerTime { UpdatedAt = _clock.SwedenNow, UpdatedBy = userId, ImpersonatorUpdatedBy = impersonatingUserId };

            // Log and notify
            _logger.LogInformation($"Expiry {expiry} manually set on request {request.RequestId}");
            _notificationService.RequestCreated(request);
        }

        public DisplayPriceInformation GetOrderPriceinformationForConfirmation(Order order, PriceListType pl)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(GetOrderPriceinformationForConfirmation), nameof(OrderService));
            CompetenceLevel cl = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(SelectCompetenceLevelForPriceEstimation(order.CompetenceRequirements?.Select(item => item.CompetenceLevel)));
            int rankingId = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date)
                .Where(r => !r.Quarantines.Any(q => q.CustomerOrganisationId == order.CustomerOrganisationId && q.ActiveFrom <= _clock.SwedenNow && q.ActiveTo >= _clock.SwedenNow))
                .OrderBy(r => r.Rank).FirstOrDefault().RankingId;
            return PriceCalculationService.GetPriceInformationToDisplay(_priceCalculationService.GetPrices(order.StartAt, order.EndAt, cl, pl, rankingId, order.CreatedAt).PriceRows);
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
            if (list == null || !list.Any())
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task CleanTempAttachments()
        {
            using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    _logger.LogInformation("Cleaning temporary attachments");
                    // Find attachmentgroups older than 24 hours
                    var attachmentsGroupsToDelete = await _tolkDbContext.TemporaryAttachmentGroups.Where(ta => ta.CreatedAt < _clock.SwedenNow.AddDays(-1)).ToListAsync();
                    if (attachmentsGroupsToDelete.Any())
                    {
                        _logger.LogInformation("Cleaning {0} attachmentgroups", attachmentsGroupsToDelete.Count);
                        _tolkDbContext.TemporaryAttachmentGroups.RemoveRange(attachmentsGroupsToDelete);
                        await _tolkDbContext.SaveChangesAsync();
                    }

                    // Find orphaned attachments
                    var attachmentsToDelete = await _tolkDbContext.Attachments.Where(a => !_tolkDbContext.TemporaryAttachmentGroups.Select(ta => ta.AttachmentId).Contains(a.AttachmentId))
                                                                        .Where(a => !_tolkDbContext.OrderAttachments.Select(oa => oa.AttachmentId).Contains(a.AttachmentId))
                                                                        .Where(a => !_tolkDbContext.RequestAttachments.Select(ra => ra.AttachmentId).Contains(a.AttachmentId))
                                                                        .Where(a => !_tolkDbContext.RequisitionAttachments.Select(ra => ra.AttachmentId).Contains(a.AttachmentId)).ToListAsync();
                    if (attachmentsToDelete.Any())
                    {
                        _logger.LogInformation("Cleaning {0} attachments", attachmentsToDelete.Count);
                        _tolkDbContext.Attachments.RemoveRange(attachmentsToDelete);
                        await _tolkDbContext.SaveChangesAsync();
                    }
                    if (attachmentsGroupsToDelete.Any() || attachmentsToDelete.Any())
                    {
                        trn.Commit();
                    }
                    _logger.LogInformation("Done cleaning temporary attachments");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing {methodName}", nameof(CleanTempAttachments));
                    trn.Rollback();
                    await SendErrorMail(nameof(CleanTempAttachments), ex);
                }
            }
        }

        private async Task<RequestGroup> GetNewRequestGroup(int requestGroupId)
        {
            return await _tolkDbContext.RequestGroups
                .Include(g => g.OrderGroup).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(g => g.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .Include(g => g.OrderGroup).ThenInclude(o => o.CustomerUnit)
                .Include(g => g.OrderGroup).ThenInclude(o => o.Region)
                .Include(g => g.OrderGroup).ThenInclude(o => o.Language)
                .Include(g => g.OrderGroup).ThenInclude(r => r.Orders).ThenInclude(o => o.InterpreterLocations)
                .Include(g => g.OrderGroup).ThenInclude(o => o.CompetenceRequirements)
                .Include(g => g.OrderGroup).ThenInclude(o => o.Requirements)
                .Include(g => g.OrderGroup).ThenInclude(o => o.CreatedByUser)
                .Include(g => g.OrderGroup).ThenInclude(r => r.Orders).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceCalculationCharge)
                .Include(g => g.OrderGroup).ThenInclude(r => r.Orders).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(g => g.OrderGroup).ThenInclude(r => r.Orders).ThenInclude(o => o.IsExtraInterpreterForOrder)
                .Include(r => r.Ranking.Broker)
                .SingleAsync(r => r.RequestGroupId == requestGroupId);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        private async Task HandleStartedOrders()
        {
            var requestIds = await _tolkDbContext.Requests
                .Where(r => (r.Order.StartAt <= _clock.SwedenNow && r.Order.Status == OrderStatus.ResponseAccepted) &&
                     r.Status == RequestStatus.Approved && r.InterpreterCompetenceVerificationResultOnAssign != null &&
                     r.InterpreterCompetenceVerificationResultOnStart == null)
                .Select(r => r.RequestId)
                .ToListAsync();

            _logger.LogInformation("Found {count} requests where the interpreter should be revalidated: {requestIds}",
                requestIds.Count, string.Join(", ", requestIds));

            foreach (var requestId in requestIds)
            {
                try
                {
                    var startedRequest = await _tolkDbContext.Requests
                    .Include(r => r.Interpreter)
                    .SingleOrDefaultAsync(r => r.RequestId == requestId);
                    var officialInterpreterId = startedRequest.Interpreter.OfficialInterpreterId;
                    if (startedRequest == null)
                    {
                        _logger.LogInformation("Request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                            requestId);
                    }
                    else
                    {
                        if (_tolkBaseOptions.Tellus.IsActivated)
                        {
                            _logger.LogInformation("Processing started request {requestId} for Order {orderId}.",
                                startedRequest.RequestId, startedRequest.OrderId);
                            startedRequest.InterpreterCompetenceVerificationResultOnStart = await _verificationService.VerifyInterpreter(officialInterpreterId, startedRequest.OrderId, (CompetenceAndSpecialistLevel)startedRequest.CompetenceLevel, true);
                            await _tolkDbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing revalidation-request {requestId}", requestId);
                    await SendErrorMail(nameof(HandleStartedOrders), ex);
                }
            }
        }

        private async Task TerminateOrderGroup(OrderGroup orderGroup)
        {
            foreach (Order order in orderGroup.Orders)
            {
                await TerminateOrder(order, false);

            }
            var terminatedOrderGroup = await _tolkDbContext.OrderGroups
              .Include(o => o.CreatedByUser)
              .Include(o => o.Orders).ThenInclude(o => o.CustomerUnit)
              .SingleAsync(o => o.OrderGroupId == orderGroup.OrderGroupId);
            _notificationService.OrderGroupNoBrokerAccepted(terminatedOrderGroup);
            _logger.LogInformation("Could not create another request group for order group {orderGroupId}, no more available brokers or too close in time.",
               orderGroup.OrderGroupId);
        }

        private async Task TerminateOrder(Order order, bool notify = true)
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
            if (notify)
            {
                var terminatedOrder = await _tolkDbContext.Orders
                   .Include(o => o.CreatedByUser)
                   .Include(o => o.ContactPersonUser)
                   .Include(o => o.CustomerUnit)
                   .SingleAsync(o => o.OrderId == order.OrderId);
                _notificationService.OrderNoBrokerAccepted(terminatedOrder);
                _logger.LogInformation("Could not create another request for order {orderId}, no more available brokers or too close in time.",
                    order.OrderId);
            }
        }

        private void CreatePriceInformation(OrderGroup orderGroup)
        {
            orderGroup.Orders.ForEach(o => CreatePriceInformation(o));
        }

        private void CreatePriceInformation(Order order)
        {
            _logger.LogInformation("Create price rows for Order: {orderId}, Customer: {Name}",
                order?.OrderId, order?.CustomerOrganisation?.Name);
            var priceInformation = _priceCalculationService.GetPrices(
                order.StartAt,
                order.EndAt,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(SelectCompetenceLevelForPriceEstimation(order.CompetenceRequirements?.Select(item => item.CompetenceLevel))),
                order.CustomerOrganisation.PriceListType,
                order.Requests.Single(r => r.IsToBeProcessedByBroker || r.IsAcceptedOrApproved).RankingId,
                order.CreatedAt);
            order.PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, OrderPriceRow>(row)));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        private async Task HandleExpiredRequests()
        {
            var expiredRequestIds = await _tolkDbContext.Requests
                .Where(r => r.RequestGroupId == null &&
                    ((r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)) ||
                    (r.Order.StartAt <= _clock.SwedenNow && r.Status == RequestStatus.AwaitingDeadlineFromCustomer)))
                .Select(r => r.RequestId)
                .ToListAsync();

            _logger.LogInformation("Found {count} expired requests to process: {requestIds}",
                expiredRequestIds.Count, string.Join(", ", expiredRequestIds));

            foreach (var requestId in expiredRequestIds)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var expiredRequest = await _tolkDbContext.Requests
                            .Include(r => r.Ranking)
                            .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                            .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Quarantines)
                            .SingleOrDefaultAsync(r =>
                                ((r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received))
                                || (r.Order.StartAt <= _clock.SwedenNow && r.Status == RequestStatus.AwaitingDeadlineFromCustomer))
                                && r.RequestId == requestId);

                        if (expiredRequest == null)
                        {
                            _logger.LogInformation("Request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                requestId);
                        }
                        else
                        {
                            _logger.LogInformation("Processing expired request {requestId} for Order {orderId}.",
                                expiredRequest.RequestId, expiredRequest.OrderId);

                            var requestAwaitingDeadlineFromCustomer = expiredRequest.Status == RequestStatus.AwaitingDeadlineFromCustomer;
                            expiredRequest.Status = RequestStatus.DeniedByTimeLimit;

                            if (expiredRequest.Order.StartAt <= _clock.SwedenNow)
                            {
                                if (requestAwaitingDeadlineFromCustomer)
                                {
                                    expiredRequest.Status = RequestStatus.NoDeadlineFromCustomer;
                                }
                                else
                                {
                                    _notificationService.RequestExpired(expiredRequest);
                                }
                                await TerminateOrder(expiredRequest.Order);
                            }
                            else
                            {
                                _notificationService.RequestExpired(expiredRequest);
                                await CreateRequest(expiredRequest.Order, expiredRequest);
                            }
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing expired request {requestId}", requestId);
                        trn.Rollback();
                        await SendErrorMail(nameof(HandleExpiredRequests), ex);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        private async Task HandleExpiredRequestGroups()
        {
            var expiredRequestGroupIds = await _tolkDbContext.RequestGroups
                .Include(r => r.OrderGroup).ThenInclude(o => o.Orders)
                .Where(r => (r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)) ||
                    (r.OrderGroup.ClosestStartAt <= _clock.SwedenNow && r.Status == RequestStatus.AwaitingDeadlineFromCustomer))
                .Select(r => r.RequestGroupId)
                .ToListAsync();

            _logger.LogInformation("Found {count} expired request groups to process: {requestGroupIds}",
                expiredRequestGroupIds.Count, string.Join(", ", expiredRequestGroupIds));
            foreach (var requestGroupId in expiredRequestGroupIds)
            {
                using (var trn = _tolkDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var expiredRequestGroup = await _tolkDbContext.RequestGroups
                            .Include(r => r.Ranking)
                            .Include(g => g.OrderGroup).ThenInclude(r => r.Orders).ThenInclude(o => o.CustomerOrganisation)
                            .Include(g => g.OrderGroup).ThenInclude(r => r.Orders).ThenInclude(o => o.Requests)
                            .Include(g => g.OrderGroup).ThenInclude(r => r.RequestGroups).ThenInclude(r => r.Ranking)
                            .Include(g => g.Requests).ThenInclude(r => r.Ranking)
                            .SingleOrDefaultAsync(r =>
                                ((r.ExpiresAt <= _clock.SwedenNow && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received))
                                || (r.OrderGroup.ClosestStartAt <= _clock.SwedenNow && r.Status == RequestStatus.AwaitingDeadlineFromCustomer))
                                && r.RequestGroupId == requestGroupId);

                        if (expiredRequestGroup == null)
                        {
                            _logger.LogInformation("Request group {requestGroupId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                requestGroupId);
                        }
                        else
                        {
                            _logger.LogInformation("Processing expired request {requestId} for Order group {orderGroupId}.",
                                expiredRequestGroup.RequestGroupId, expiredRequestGroup.OrderGroupId);

                            expiredRequestGroup.SetStatus(RequestStatus.DeniedByTimeLimit);

                            if (expiredRequestGroup.OrderGroup.ClosestStartAt <= _clock.SwedenNow)
                            {
                                expiredRequestGroup.SetStatus(RequestStatus.NoDeadlineFromCustomer);
                                await TerminateOrderGroup(expiredRequestGroup.OrderGroup);
                            }
                            else
                            {
                                _notificationService.RequestGroupExpired(expiredRequestGroup);
                                await CreateRequestGroup(expiredRequestGroup.OrderGroup, expiredRequestGroup);
                            }

                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing expired request group {requestGroupId}", requestGroupId);
                        trn.Rollback();
                        await SendErrorMail(nameof(HandleExpiredRequestGroups), ex);
                    }
                }
            }
        }

        private async Task SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendErrorEmail(nameof(OrderService), methodname, ex);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        private async Task HandleExpiredNonAnsweredRespondedRequests()
        {
            var nonAnsweredRespondedRequestsId = await _tolkDbContext.Requests
                .Where(r => r.RequestGroupId == null &&
                    (r.Order.Status == OrderStatus.RequestResponded || r.Order.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                    r.Order.StartAt <= _clock.SwedenNow && (r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed))
                .Select(r => r.RequestId).ToListAsync();

            _logger.LogInformation("Found {count} non answered responded requests that expires: {requestIds}",
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
                            _logger.LogInformation("Non answered responded request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                                requestId);
                        }
                        else
                        {
                            _logger.LogInformation("Set new status for non answered responded request {requestId}.",
                                requestId);
                            request.Status = RequestStatus.ResponseNotAnsweredByCreator;
                            request.Order.Status = OrderStatus.ResponseNotAnsweredByCreator;
                            await _tolkDbContext.SaveChangesAsync();
                            trn.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing {methodName} for request {requestId}", nameof(HandleExpiredNonAnsweredRespondedRequests), requestId);
                        trn.Rollback();
                        await SendErrorMail(nameof(HandleExpiredNonAnsweredRespondedRequests), ex);
                    }
                }
            }
        }

    }
}
