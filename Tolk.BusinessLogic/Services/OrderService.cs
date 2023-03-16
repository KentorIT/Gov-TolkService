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
        private readonly CacheService _cacheService;

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
            ITolkBaseOptions tolkBaseOptions,
            CacheService cacheService
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
            _cacheService = cacheService;
        }

        public async Task HandleAllScheduledTasks()
        {
            await HandleStartedOrders();
            await HandleCompletedRequests();
            await HandleExpiredRequests();
            await HandleExpiredRequestGroups();
            await HandleExpiredNonAnsweredRespondedRequests();
            await HandleExpiredNonAnsweredRespondedRequestGroups();
        }

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
                using var trn = await _tolkDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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
                        await trn.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing expired complaint {complaintId}", complaintId);
                    await trn.RollbackAsync();
                    await SendErrorMail(nameof(HandleExpiredComplaints), ex);
                }
            }
        }

        public void ChangeContactPerson(Order order, int? newContactPersonId, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(ChangeContactPerson), nameof(OrderService));
            order.OrderChangeLogEntries = new List<OrderChangeLogEntry>();
            order.ChangeContactPerson(_clock.SwedenNow, userId,
                impersonatorId, _tolkDbContext.Users.SingleOrDefault(u => u.Id == newContactPersonId));
        }

        public async Task Create(Order order, DateTimeOffset? latestAnswerBy)
        {
            //Validations to make
            //min 1 max 2 competence levels if required
            //min 0 max 2 competence levels if NOT required
            // all competence levels must have a unique rank
            //min 1 max 3 locations
            // all locations must have a unique rank
            //Must there be an lastestanswerby set?
            //is latestAnswerBy valid, i.e. before start at? 

            //Updates to the order if specific rules apply: (Move from OrderModel)
            //Ignore LastAnswerBy, if regular rules apply.
            //Ignore competence levels if language does not have this. If so, set the competence level to OtherInterpreter
            //if no competence level is provided and the language HAS authorization, set AuthorizedInterpreter
            // the two above is used for price calculation...
            //Ignore meal breaks if the occasion is shorter than x minutes.
            //Ignore Allow exceeding cost if the locations are all off site.
            //NOTE: This method should probably return a list of "corrections" that was made to the order, according to the above rules.
            // This can then be returned to the api call that created the order...
            await _tolkDbContext.AddAsync(order);
            await _tolkDbContext.SaveChangesAsync(); // Save changes to get id for event log

            await CreateRequest(order, latestAnswerBy: latestAnswerBy);
        }

        public async Task CreateRequestGroup(OrderGroup group, RequestGroup expiredRequestGroup = null, DateTimeOffset? latestAnswerBy = null)
        {
            NullCheckHelper.ArgumentCheckNull(group, nameof(CreateRequestGroup), nameof(OrderService));
            var currentFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            if (!currentFrameworkAgreement.IsActive)
            {
                await TerminateOrderGroup(group);
            }
            else
            {
                RequestGroup requestGroup = null;
                var calculatedExpiry = CalculateExpiryForNewRequest(group.ClosestStartAt, currentFrameworkAgreement.FrameworkAgreementResponseRuleset, latestAnswerBy);

                if ((!calculatedExpiry.ExpiryAt.HasValue && !group.IsSingleOccasion) || (expiredRequestGroup?.IsTerminalRequest ?? false))
                {
                    //Does not handle no expiry for several occasions order.
                    group.SetStatus(OrderStatus.NoBrokerAcceptedOrder);
                    await TerminateOrderGroup(group);
                    await _tolkDbContext.SaveChangesAsync();
                    return;
                }
                var rankings = _rankingService.GetActiveRankingsForRegion(group.RegionId, group.ClosestStartAt.Date, currentFrameworkAgreement.LastValidDate);

                if (expiredRequestGroup != null)
                {
                    requestGroup = group.CreateRequestGroup(rankings, calculatedExpiry, _clock.SwedenNow);
                }
                else
                {
                    requestGroup = group.CreateRequestGroup(rankings, calculatedExpiry, _clock.SwedenNow, latestAnswerBy.HasValue);
                    //This is the first time a request is created on this order, add the priceinformation too...
                    await _tolkDbContext.SaveChangesAsync();
                    CreatePriceInformation(group, requestGroup.Ranking.FrameworkAgreement.BrokerFeeCalculationType);
                }

                // Save to get ids for the log message.
                await _tolkDbContext.SaveChangesAsync();

                if (requestGroup != null)
                {
                    RequestGroup newRequestGroup = await GetNewRequestGroup(requestGroup.RequestGroupId);
                    if (calculatedExpiry.ExpiryAt.HasValue)
                    {
                        _logger.LogInformation("Created request group {requestGroupId} for order group {orderGroupId} to {brokerId} with expiry {expiry}",
                            requestGroup.RequestGroupId, requestGroup.OrderGroupId, requestGroup.Ranking.BrokerId, requestGroup.ExpiresAt);
                        await _notificationService.RequestGroupCreated(newRequestGroup);
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
                        }
                        else
                        {
                            throw new NotImplementedException("Not a valid state!");
                        }
                    }
                }
                else
                {
                    await TerminateOrderGroup(group);
                }
            }
        }

        public async Task CreatePartialRequestGroup(RequestGroup requestGroup, IEnumerable<Request> declinedRequests)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(CreatePartialRequestGroup), nameof(OrderService));
            NullCheckHelper.ArgumentCheckNull(declinedRequests, nameof(CreatePartialRequestGroup), nameof(OrderService));
            var currentFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            if (requestGroup.IsTerminalRequest)
            {
                //Possibly a terminatepartial, with its own notification to customer?
                throw new NotImplementedException("Vet inte riktigt vart vi skall ta vägen om det här händer...");
            }
            var calculatedExpiry = CalculateExpiryForNewRequest(declinedRequests.ClosestStartAt(), currentFrameworkAgreement.FrameworkAgreementResponseRuleset);

            OrderGroup group = requestGroup.OrderGroup;

            var rankings = _rankingService.GetActiveRankingsForRegion(group.RegionId, group.ClosestStartAt.Date, currentFrameworkAgreement.LastValidDate);
            RequestGroup partialRequestGroup = group.CreatePartialRequestGroup(declinedRequests, rankings, calculatedExpiry, _clock.SwedenNow);
            // Save to get ids for the log message.
            await _tolkDbContext.SaveChangesAsync();

            if (partialRequestGroup != null)
            {
                RequestGroup newRequestGroup = await GetNewRequestGroup(partialRequestGroup.RequestGroupId);
                if (calculatedExpiry.ExpiryAt.HasValue)
                {
                    _logger.LogInformation("Created request group {requestGroupId} for order group {orderGroupId} to {brokerId} with expiry {expiry}",
                        partialRequestGroup.RequestGroupId, partialRequestGroup.OrderGroupId, partialRequestGroup.Ranking.BrokerId, partialRequestGroup.ExpiresAt);
                    await _notificationService.RequestGroupCreated(newRequestGroup);
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
            var currentFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            FrameworkAgreement orderFrameworkAgreement = null;

            Request request = null;
            order.Requests = await _tolkDbContext.Requests.GetRequestsForOrder(order.OrderId).ToListAsync();
            if (order.Requests.Any())
            {
                orderFrameworkAgreement = order.Requests.Last().Ranking.FrameworkAgreement;
            }
            //if not first request - check if agreement connected to order is active, if not terminate
            if ((orderFrameworkAgreement != null && !orderFrameworkAgreement.IsActive(_clock.SwedenNow)) ||
               //else check if current agreement is active, if not terminate
               (orderFrameworkAgreement == null && !currentFrameworkAgreement.IsActive))
            {
                order.Status = OrderStatus.NoBrokerAcceptedOrder;
                await TerminateOrder(order, notify);
            }
            //else check if new request can be created
            else
            {
                var calculatedExpiry = CalculateExpiryForNewRequest(order.StartAt, currentFrameworkAgreement.FrameworkAgreementResponseRuleset, latestAnswerBy);
                var rankings = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date, currentFrameworkAgreement.LastValidDate);
                if (expiredRequest != null)
                {
                    // Only create a new request if the previous request was not a flagged as terminal.
                    if (!expiredRequest.IsTerminalRequest)
                    {
                        request = order.CreateRequest(rankings, calculatedExpiry, _clock.SwedenNow);
                    }
                }
                else
                {
                    request = order.CreateRequest(rankings, calculatedExpiry, _clock.SwedenNow, latestAnswerBy.HasValue);
                    //This is the first time a request is created on this order, add the priceinformation too...
                    await _tolkDbContext.SaveChangesAsync();
                    CreatePriceInformation(order, request.Ranking.FrameworkAgreement.BrokerFeeCalculationType);
                }

                // Save to get ids for the log message.
                await _tolkDbContext.SaveChangesAsync();

                if (request != null)
                {
                    var newRequest = await _tolkDbContext.Requests.GetRequestForNewRequestById(request.RequestId);
                    newRequest.Order.InterpreterLocations = await _tolkDbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.OrderId).ToListAsync();
                    if (calculatedExpiry.ExpiryAt.HasValue)
                    {
                        _logger.LogInformation("Created request {requestId} for order {orderId} to {brokerId} with expiry {expiry}",
                            request.RequestId, request.OrderId, request.Ranking.BrokerId, request.ExpiresAt);
                        if (notify)
                        {
                            if (request.Order.ExpectedLength.HasValue)
                            {
                                await _notificationService.FlexibleRequestCreated(newRequest);
                            }
                            else
                            {
                                switch (EnumHelper.Parent<RequestAnswerRuleType, RequiredAnswerLevel>(request.RequestAnswerRuleType))
                                {
                                    case RequiredAnswerLevel.Full:
                                        await _notificationService.RequestNeedsFullAnswerCreated(newRequest);
                                        break;
                                    case RequiredAnswerLevel.Acceptance:
                                        await _notificationService.RequestNeedsAcceptanceCreated(newRequest);
                                        break;
                                    default:
                                        throw new InvalidOperationException();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Request expiry information from customer
                        order.Status = OrderStatus.AwaitingDeadlineFromCustomer;
                        request.Status = RequestStatus.AwaitingDeadlineFromCustomer;
                        request.IsTerminalRequest = true;

                        await _tolkDbContext.SaveChangesAsync();

                        _logger.LogInformation("Created request {requestId} for order {orderId}, but system was unable to calculate expiry.", request.RequestId, request.OrderId);
                        if (notify)
                        {
                            _notificationService.RequestCreatedWithoutExpiry(newRequest);
                        }
                    }
                }
                else
                {
                    order.Status = OrderStatus.NoBrokerAcceptedOrder;
                    await TerminateOrder(order, notify);
                }
            }
        }

        public async Task ReplaceOrder(Order order, Order replacementOrder, int userId, int? impersonatorId, string cancelMessage)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(ReplaceOrder), nameof(OrderService));
            NullCheckHelper.ArgumentCheckNull(replacementOrder, nameof(ReplaceOrder), nameof(OrderService));
            var request = await _tolkDbContext.Requests.GetActiveRequestByOrderId(order.OrderId);
            if (request == null)
            {
                throw new InvalidOperationException($"Order {order.OrderId} has no active requests that can be cancelled");
            }
            var replacingRequest = new Request(request, new RequestExpiryResponse { ExpiryAt = order.StartAt, RequestAnswerRuleType = RequestAnswerRuleType.ReplacedOrder }, _clock.SwedenNow);
            await _tolkDbContext.AddAsync(replacementOrder);
            replacingRequest.Order = replacementOrder;
            await _tolkDbContext.AddAsync(replacingRequest);
            await CancelOrder(order, userId, impersonatorId, cancelMessage, true);
            replacementOrder.CreatedAt = _clock.SwedenNow;
            replacementOrder.CreatedBy = userId;
            replacementOrder.ImpersonatingCreator = impersonatorId;

            //Generate new price rows from current times, might be subject to change!!!
            CreatePriceInformation(replacementOrder, request.Ranking.FrameworkAgreement.BrokerFeeCalculationType);
            await _tolkDbContext.SaveChangesAsync();

            replacementOrder.Requirements = await _tolkDbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(request.RequestId).Select(a => new OrderRequirement
            {
                Description = a.OrderRequirement.Description,
                IsRequired = a.OrderRequirement.IsRequired,
                RequirementType = a.OrderRequirement.RequirementType,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>
                {
                    new OrderRequirementRequestAnswer
                    {
                        Answer = a.Answer,
                        CanSatisfyRequirement = a.CanSatisfyRequirement,
                        RequestId = replacingRequest.RequestId
                    }
                }
            }).ToListAsync();
            await _tolkDbContext.SaveChangesAsync();
            await _notificationService.OrderReplacementCreated(request.RequestId, replacingRequest.RequestId);
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

        public async Task ApproveRequestGroupAnswer(RequestGroup requestGroup, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(ApproveRequestGroupAnswer), nameof(OrderService));
            requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            requestGroup.Approve(_clock.SwedenNow, userId, impersonatorId);

            _notificationService.RequestGroupAnswerApproved(requestGroup);
        }

        public async Task DenyRequestAnswer(Request request, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(DenyRequestAnswer), nameof(OrderService));
            var createRequest = !request.TerminateOnDenial;
            request.Deny(_clock.SwedenNow, userId, impersonatorId, message);
            var order = request.Order;
            if (createRequest)
            {
                await CreateRequest(order, request);
            }
            else
            {
                request.Order.Status = OrderStatus.NoBrokerAcceptedOrder;
                await TerminateOrder(order);
            }
            _notificationService.RequestAnswerDenied(request);
        }

        public async Task DenyRequestGroupAnswer(RequestGroup requestGroup, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(DenyRequestGroupAnswer), nameof(OrderService));
            requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.OrderGroup.RequestGroups = await _tolkDbContext.RequestGroups.GetRequestGroupsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
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
            order.OrderStatusConfirmations = await _tolkDbContext.OrderStatusConfirmation.GetStatusConfirmationsForOrder(order.OrderId).ToListAsync();
            order.ConfirmNoAnswer(_clock.SwedenNow, userId, impersonatorId);
        }

        public async Task ConfirmResponeNotAnswered(Order order, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(ConfirmResponeNotAnswered), nameof(OrderService));
            order.ConfirmResponseNotAnswered(_clock.SwedenNow, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmGroupNoAnswer(OrderGroup orderGroup, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(ConfirmGroupNoAnswer), nameof(OrderService));
            orderGroup.StatusConfirmations = await _tolkDbContext.OrderGroupStatusConfirmations.GetStatusConfirmationsForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.Orders.ForEach(r => r.OrderStatusConfirmations = new List<OrderStatusConfirmation>());

            orderGroup.ConfirmNoAnswer(_clock.SwedenNow, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmGroupResponeNotAnswered(OrderGroup orderGroup, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(ConfirmGroupResponeNotAnswered), nameof(OrderService));
            orderGroup.StatusConfirmations = await _tolkDbContext.OrderGroupStatusConfirmations.GetStatusConfirmationsForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.Orders.ForEach(r => r.OrderStatusConfirmations = new List<OrderStatusConfirmation>());
            orderGroup.ConfirmResponseNotAnswered(_clock.SwedenNow, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task CancelOrder(Order order, int userId, int? impersonatorId, string message, bool isReplaced = false)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(CancelOrder), nameof(OrderService));
            var request = await _tolkDbContext.Requests.GetActiveRequestByOrderId(order.OrderId);
            if (request == null)
            {
                throw new InvalidOperationException($"Order {order.OrderId} has no active requests that can be cancelled");
            }
            request.PriceRows = await _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync();
            request.Requisitions = new List<Requisition>();
            var now = _clock.SwedenNow;

            //check if late cancelling, if so we check for mealbreaks
            bool createFullCompensationRequisition = !isReplaced && _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(now.DateTime, order.StartAt.DateTime) < 2;
            List<RequisitionPriceRow> priceRows = null;
            List<MealBreak> mealbreaks = null;
            if (request.Status == RequestStatus.Approved && !isReplaced)
            {
                (priceRows, mealbreaks) = GetCompensationPriceRowsForCancelledRequest(request, createFullCompensationRequisition);
            }
            request.Cancel(now, userId, impersonatorId, message, createFullCompensationRequisition, isReplaced, mealbreaks: mealbreaks, priceRows: priceRows);

            if (!isReplaced)
            {
                //Only notify if the order was not replaced
                _notificationService.OrderCancelledByCustomer(request, createFullCompensationRequisition);
            }
            _logger.LogInformation("Order {orderId} cancelled by customer {userId}.", order.OrderId, userId);
        }

        public (List<RequisitionPriceRow>, List<MealBreak>) GetCompensationPriceRowsForCancelledRequest(Request request, bool createFullCompensationRequisition)
        {
            var mealbreaks = (createFullCompensationRequisition && (request.Order.MealBreakIncluded ?? false)) ? new List<MealBreak> { new MealBreak { StartAt = request.Order.StartAt.AddHours(2).ToDateTimeOffsetSweden(), EndAt = request.Order.StartAt.AddHours(3).ToDateTimeOffsetSweden() } } : null;

            //if mealbreaks and full compensation we must get correct prices with mealbreaks deducted otherwize make a copy from the request's pricerows.
            var priceRows = (createFullCompensationRequisition && mealbreaks != null) ? _priceCalculationService.GetPricesRequisition(
                request.Order.StartAt,
                request.Order.Duration,
                request.Order.StartAt,
                request.Order.Duration,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)request.CompetenceLevel),
                request.Order.CustomerOrganisation.PriceListType,
                out bool useRequestRows,
                null,
                null,
                request.PriceRows.OfType<PriceRowBase>(),
                request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price,
                null,
                request.Order.CreatedAt,
                mealbreaks
            ).PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequisitionPriceRow>(row)).ToList() :
            request.GenerateRequisitionPriceRows(createFullCompensationRequisition);
            return (priceRows, mealbreaks);
        }

        public async Task CancelOrderGroup(OrderGroup orderGroup, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(CancelOrderGroup), nameof(OrderService));
            var requestGroup = await _tolkDbContext.RequestGroups.GetActiveRequestGroupByOrderGroupId(orderGroup.OrderGroupId);
            if (requestGroup == null)
            {
                throw new InvalidOperationException($"Order group {orderGroup.OrderGroupId} has no active request group that can be cancelled");
            }
            try
            {
                requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
                requestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
                requestGroup.Cancel(_clock.SwedenNow, userId, impersonatorId, message);
                _logger.LogInformation("Order group {orderGroupId} cancelled by customer {userId}.", orderGroup.OrderGroupId, userId);
                _notificationService.OrderGroupCancelledByCustomer(requestGroup);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Failed cancelling of Order group {orderGroupId} by user {userId}.", orderGroup.OrderGroupId, userId);
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task SetRequestExpiryManually(Request request, DateTimeOffset expiry, int userId, int? impersonatingUserId)
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
            if (request.Order.ExpectedLength.HasValue)
            {
                await _notificationService.FlexibleRequestCreated(request);
            }
            else
            {
                await _notificationService.RequestNeedsFullAnswerCreated(request);
            }
        }

        public async Task SetRequestGroupExpiryManually(RequestGroup requestGroup, DateTimeOffset expiry, int userId, int? impersonatingUserId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(SetRequestGroupExpiryManually), nameof(OrderService));

            requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId).ToListAsync();

            requestGroup.SetExpiryManually(_clock.SwedenNow, expiry, userId, impersonatingUserId);
            _logger.LogInformation($"Expiry {expiry} manually set on request group {requestGroup.RequestGroupId}");
            await _notificationService.RequestGroupCreated(requestGroup);
        }

        public DisplayPriceInformation GetOrderPriceinformationForConfirmation(Order order, PriceListType pl, BrokerFeeCalculationType brokerFeeCalculationType)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(GetOrderPriceinformationForConfirmation), nameof(OrderService));
            CompetenceLevel cl = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(SelectCompetenceLevelForPriceEstimation(order.CompetenceRequirements?.Select(item => item.CompetenceLevel)));
            int rankingId = _rankingService.GetActiveRankingsForRegion(order.RegionId, order.StartAt.Date, _cacheService.CurrentOrLatestFrameworkAgreement.LastValidDate)
                .Where(r => !r.Quarantines.Any(q => q.CustomerOrganisationId == order.CustomerOrganisationId && q.ActiveFrom <= _clock.SwedenNow && q.ActiveTo >= _clock.SwedenNow))
                .OrderBy(r => r.Rank).FirstOrDefault().RankingId;
            return PriceCalculationService.GetPriceInformationToDisplay(
                _priceCalculationService.GetPrices(
                    order.StartAt,
                    order.Duration,
                    cl,
                    pl,
                    order.CreatedAt,
                    _priceCalculationService.GetCalculatedBrokerFee(order, _clock.SwedenNow, brokerFeeCalculationType, cl, rankingId, order.InterpreterLocations.OrderBy(l => l.Rank).First().InterpreterLocation))
                .PriceRows);
        }

        /// <summary>
        /// Takes an order's start time and calculates an expiry time for answering an order request.
        /// 
        /// When the appointment starts in too short of a time-frame, the system will not calculate an expiry time and return null. 
        /// When that happens: the user should manually set an expiry time.
        /// </summary>
        /// <param name="startDateTime">Time and date when the appointment starts</param>
        /// <returns>Null if startDateTime is too close in time to automatically calculate an expiry time</returns>
        public RequestExpiryResponse CalculateExpiryForNewRequest(DateTimeOffset startDateTime, FrameworkAgreementResponseRuleset ruleset, DateTimeOffset? explicitLastAnswerTime = null)
        {
            // Grab current time to not risk it flipping over during execution of the method.
            var closestWorkingDayCreatedTime = _clock.SwedenNow;
            var closestWorkingDayStartAtTime = startDateTime.DateTime.ToDateTimeOffsetSweden();
            var closestWorkingDayBeforeStart = startDateTime.Date;
            var response = new RequestExpiryResponse
            {
                ExpiryAt = explicitLastAnswerTime,
                LastAcceptedAt = null,
                RequestAnswerRuleType = RequestAnswerRuleType.ResponseSetByCustomer
            };
            //1. if sweden now/created time is weekend or holiday, move up to first workday, and set 00:00 as time
            if (!_dateCalculationService.IsWorkingDay(closestWorkingDayCreatedTime.Date))
            {
                closestWorkingDayCreatedTime = _dateCalculationService.GetFirstWorkDay(closestWorkingDayCreatedTime.Date).Date.ToDateTimeOffsetSweden();
            }
            //2. if start time for order is weekend or holiday, move back to last workday, add a day and then set 00:00 as time (this will actually be a non working day 00:00)
            if (!_dateCalculationService.IsWorkingDay(closestWorkingDayBeforeStart))
            {
                closestWorkingDayBeforeStart = _dateCalculationService.GetLastWorkDay(closestWorkingDayBeforeStart).Date;
                closestWorkingDayStartAtTime = _dateCalculationService.GetLastWorkDay(closestWorkingDayBeforeStart).AddDays(1).Date.ToDateTimeOffsetSweden();
            }
            if (closestWorkingDayCreatedTime.Date < closestWorkingDayStartAtTime.Date)
            {
                var daysInAdvance = _dateCalculationService.GetWorkDaysBetween(closestWorkingDayCreatedTime.Date, closestWorkingDayBeforeStart);
                if (ruleset == FrameworkAgreementResponseRuleset.VersionTwo && (daysInAdvance > 20 || (daysInAdvance == 20 && closestWorkingDayCreatedTime.TimeOfDay.Ticks <= closestWorkingDayStartAtTime.TimeOfDay.Ticks)))
                {
                    response.RequestAnswerRuleType = RequestAnswerRuleType.RequestCreatedMoreThanTwentyDaysBefore;
                    response.LastAcceptedAt = _dateCalculationService.GetDateForANumberOfWorkdaysinFuture(closestWorkingDayCreatedTime.DateTime, 4).ToDateTimeOffsetSweden().ClearSeconds();
                    response.ExpiryAt = _dateCalculationService.GetDateForANumberOfWorkdaysAgo(closestWorkingDayStartAtTime.DateTime, 7).ToDateTimeOffsetSweden().ClearSeconds();
                }
                else if (ruleset == FrameworkAgreementResponseRuleset.VersionTwo && daysInAdvance <= 20 && (daysInAdvance > 10 || (daysInAdvance == 10 && closestWorkingDayCreatedTime.TimeOfDay.Ticks <= closestWorkingDayStartAtTime.TimeOfDay.Ticks)))
                {
                    response.RequestAnswerRuleType = RequestAnswerRuleType.RequestCreatedMoreThanTenDaysBefore;
                    response.LastAcceptedAt = _dateCalculationService.GetDateForANumberOfWorkdaysinFuture(closestWorkingDayCreatedTime.DateTime, 2).ToDateTimeOffsetSweden().ClearSeconds();
                    response.ExpiryAt = _dateCalculationService.GetDateForANumberOfWorkdaysAgo(closestWorkingDayStartAtTime.DateTime, 5).ToDateTimeOffsetSweden().ClearSeconds();
                }
                else if (daysInAdvance >= 2 &&
                        (ruleset == FrameworkAgreementResponseRuleset.VersionOne ||
                        (ruleset == FrameworkAgreementResponseRuleset.VersionTwo &&
                            (daysInAdvance < 10 || (daysInAdvance == 10 &&
                                closestWorkingDayCreatedTime.TimeOfDay.Ticks > closestWorkingDayStartAtTime.TimeOfDay.Ticks))
                    )))
                {
                    response.RequestAnswerRuleType = RequestAnswerRuleType.AnswerRequiredNextDay;
                    response.ExpiryAt = _dateCalculationService.GetFirstWorkDay(closestWorkingDayCreatedTime.Date.AddDays(1)).AddHours(15).ToDateTimeOffsetSweden();
                }
                else if (daysInAdvance == 1 && closestWorkingDayCreatedTime.Hour < 14)
                {
                    response.RequestAnswerRuleType = RequestAnswerRuleType.RequestCreatedOneDayBefore;
                    response.ExpiryAt = closestWorkingDayCreatedTime.Date.Add(new TimeSpan(16, 30, 0)).ToDateTimeOffsetSweden();
                }
            }
            // Starts too soon to automatically calculate response expiry time. Customer must define a dead-line manually.
            return response;
        }

        /// <summary>
        /// Used for UI validation, when to require last answer by time from customer
        /// </summary>
        public DateTime GetLastTimeForRequiringLatestAnswerBy(DateTime now)
        {
            var lastTimeForRequiringLatestAnswerBy = _dateCalculationService.GetFirstWorkDay(now).Date;
            var dateIsWorkingDay = _dateCalculationService.IsWorkingDay(now);
            //if a workday after 14:00 set to next work day
            if (dateIsWorkingDay && now.Hour >= 14)
            {
                //Add day if after 14 if order creation is a working day...
                lastTimeForRequiringLatestAnswerBy = _dateCalculationService.GetFirstWorkDay(lastTimeForRequiringLatestAnswerBy.AddDays(1).Date).Date;
            }
            //if lastTimeForRequiringLatestAnswerBy is just before a non working day, get the last non working day before first working day
            var isLastTimeForRequiringLatestAnswerBy = !_dateCalculationService.IsWorkingDay(lastTimeForRequiringLatestAnswerBy.AddDays(1));
            return isLastTimeForRequiringLatestAnswerBy ? _dateCalculationService.GetFirstWorkDay(lastTimeForRequiringLatestAnswerBy.AddDays(1).Date).Date.AddDays(-1) : lastTimeForRequiringLatestAnswerBy;
        }

        /// <summary>
        /// Used for UI validation, if clock passes 14:00 during order creation set the next last answer by time from customer
        /// </summary>
        public DateTime GetNextLastTimeForRequiringLatestAnswerBy(DateTime lastTimeForRequiringLatestAnswerBy, DateTime now)
        {
            //should onnly by increased if time is before 14:00 and the order is created a workday (else it should be the same date as lastTimeForRequiringLatestAnswerBy)
            //if a workday and before 14:00 GetLastTimeForRequiringLatestAnswerBy for the same day but after 14:00
            return (now.Hour >= 14 || !_dateCalculationService.IsWorkingDay(now)) ? lastTimeForRequiringLatestAnswerBy : GetLastTimeForRequiringLatestAnswerBy(lastTimeForRequiringLatestAnswerBy.Date.AddHours(14));
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

        public async Task CleanTempAttachments()
        {
            using var trn = await _tolkDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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
                var attachmentsToDelete = await _tolkDbContext.Attachments
                    .Where(a => !_tolkDbContext.TemporaryAttachmentGroups.Select(ta => ta.AttachmentId).Contains(a.AttachmentId))
                    .Where(a => !_tolkDbContext.OrderAttachments.Select(oa => oa.AttachmentId).Contains(a.AttachmentId))
                    .Where(a => !_tolkDbContext.RequestAttachments.Select(ra => ra.AttachmentId).Contains(a.AttachmentId))
                    .Where(a => !_tolkDbContext.OrderAttachmentHistoryEntries.Select(oah => oah.AttachmentId).Contains(a.AttachmentId))
                    .Where(a => !_tolkDbContext.OrderGroupAttachments.Select(oga => oga.AttachmentId).Contains(a.AttachmentId))
                    .Where(a => !_tolkDbContext.RequestGroupAttachments.Select(rga => rga.AttachmentId).Contains(a.AttachmentId))
                    .Where(a => !_tolkDbContext.RequisitionAttachments.Select(ra => ra.AttachmentId).Contains(a.AttachmentId)).ToListAsync();
                if (attachmentsToDelete.Any())
                {
                    _logger.LogInformation("Cleaning {0} attachments", attachmentsToDelete.Count);
                    _tolkDbContext.Attachments.RemoveRange(attachmentsToDelete);
                    await _tolkDbContext.SaveChangesAsync();
                }
                if (attachmentsGroupsToDelete.Any() || attachmentsToDelete.Any())
                {
                    await trn.CommitAsync();
                }
                _logger.LogInformation("Done cleaning temporary attachments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failure processing {methodName}", nameof(CleanTempAttachments));
                await trn.RollbackAsync();
                await SendErrorMail(nameof(CleanTempAttachments), ex);
            }
        }

        private async Task<RequestGroup> GetNewRequestGroup(int requestGroupId)
        {
            var requestGroup = await _tolkDbContext.RequestGroups.GetRequestGroupForCreateById(requestGroupId);
            requestGroup.OrderGroup.Attachments = await _tolkDbContext.OrderGroupAttachments.GetAttachmentsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            requestGroup.OrderGroup.Requirements = await _tolkDbContext.OrderGroupRequirements.GetRequirementsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            requestGroup.OrderGroup.CompetenceRequirements = await _tolkDbContext.OrderGroupCompetenceRequirements.GetOrderedCompetenceRequirementsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            requestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId, true).ToListAsync();
            await AddOrderBaseListsForGroup(requestGroup.OrderGroup, false, false);
            return requestGroup;
        }

        private async Task<OrderGroup> AddOrderBaseListsForGroup(OrderGroup orderGroup, bool includeOrderRequirements, bool includeCompetenceRequirements)
        {
            var pricerows = await _tolkDbContext.OrderPriceRows.GetPriceRowsForOrdersInOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            var locations = await _tolkDbContext.OrderInterpreterLocation.GetInterpreterLocationsForOrdersInGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.Orders.ForEach(o => o.PriceRows = pricerows.Where(p => p.OrderId == o.OrderId).ToList());
            orderGroup.Orders.ForEach(o => o.InterpreterLocations = locations.Where(l => l.OrderId == o.OrderId).ToList());
            if (includeOrderRequirements)
            {
                var requirements = await _tolkDbContext.OrderRequirements.GetRequirementsForOrdersInOrderGroup(orderGroup.OrderGroupId).ToListAsync();
                orderGroup.Orders.ForEach(o => o.Requirements = requirements.Where(r => r.OrderId == o.OrderId).ToList());
            }
            if (includeCompetenceRequirements)
            {
                var competenceRequirements = await _tolkDbContext.OrderCompetenceRequirements.GetOrderCompetencesForOrdersInOrderGroup(orderGroup.OrderGroupId).ToListAsync();
                orderGroup.Orders.ForEach(o => o.CompetenceRequirements = competenceRequirements.Where(r => r.OrderId == o.OrderId).ToList());
            }
            return orderGroup;
        }

        public async Task<OrderGroup> AddOrdersWithListsForGroup(OrderGroup orderGroup)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(AddOrdersWithListsForGroup), nameof(OrderService));
            orderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(orderGroup.OrderGroupId, true).ToListAsync();
            await AddOrderBaseListsForGroup(orderGroup, true, false);
            await AddConfirmationListsToOrdersAndGroup(orderGroup);
            return orderGroup;
        }

        public async Task<OrderGroup> AddOrdersWithListsForGroupToProcess(OrderGroup orderGroup)
        {
            NullCheckHelper.ArgumentCheckNull(orderGroup, nameof(AddOrdersWithListsForGroupToProcess), nameof(OrderService));
            orderGroup.Orders = await _tolkDbContext.Orders.GetOrdersWithIncludesForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            await AddOrderBaseListsForGroup(orderGroup, true, true);
            return orderGroup;
        }

        private async Task<OrderGroup> AddConfirmationListsToOrdersAndGroup(OrderGroup orderGroup)
        {
            orderGroup.StatusConfirmations = await _tolkDbContext.OrderGroupStatusConfirmations.GetStatusConfirmationsForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            var orderStatusConfirmations = await _tolkDbContext.OrderStatusConfirmation.GetOrderStatusConfirmationsForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.Orders.ForEach(o => o.OrderStatusConfirmations = orderStatusConfirmations.Where(osc => osc.OrderId == o.OrderId).ToList());
            return orderGroup;
        }

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
                    var startedRequest = await _tolkDbContext.Requests.GetRequestWithInterpreterById(requestId);
                    if (startedRequest == null)
                    {
                        _logger.LogInformation("Request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                            requestId);
                    }
                    else
                    {
                        var officialInterpreterId = startedRequest.Interpreter.OfficialInterpreterId;
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

        private async Task HandleCompletedRequests()
        {
            var requestIds = await _tolkDbContext.Requests.CompletedRequests(_clock.SwedenNow)
                .Select(r => r.RequestId)
                .ToListAsync();

            _logger.LogInformation("Found {count} requests that are completed: {requestIds}",
                requestIds.Count, string.Join(", ", requestIds));

            foreach (var requestId in requestIds)
            {
                try
                {
                    var completedRequest = await _tolkDbContext.Requests.GetCompletedRequestById(_clock.SwedenNow, requestId);
                    if (completedRequest == null)
                    {
                        _logger.LogInformation("Request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.", requestId);
                    }
                    else
                    {
                        _logger.LogInformation("Processing completed request {requestId} for Order {orderId}.", completedRequest.RequestId, completedRequest.OrderId);
                        _notificationService.RequestCompleted(completedRequest);
                        completedRequest.CompletedNotificationIsHandled = true;
                        await _tolkDbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing revalidation-request {requestId}", requestId);
                    await SendErrorMail(nameof(HandleCompletedRequests), ex);
                }
            }
        }

        private async Task TerminateOrderGroup(OrderGroup orderGroup)
        {
            var terminatedOrderGroup = await _tolkDbContext.OrderGroups.GetOrderGroupWithContactsById(orderGroup.OrderGroupId);
            terminatedOrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersWithUnitForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            _notificationService.OrderGroupTerminated(terminatedOrderGroup);
            _logger.LogInformation("Could not create another request group for order group {orderGroupId}, no more available brokers or too close in time.",
               orderGroup.OrderGroupId);
        }

        private async Task TerminateOrder(Order order, bool notify = true)
        {
            //The order will be terminated, send an email to tell the order creator
            if (notify)
            {
                var terminatedOrder = await _tolkDbContext.Orders.GetOrderWithContactsById(order.OrderId);
                await _notificationService.OrderTerminated(terminatedOrder);
                _logger.LogInformation("Could not create another request for order {orderId}, no more available brokers or too close in time, or it should be terminated due to rules",
                    order.OrderId);
            }
        }

        private void CreatePriceInformation(OrderGroup orderGroup, BrokerFeeCalculationType brokerFeeCalculationType)
        {
            orderGroup.Orders.ForEach(o => CreatePriceInformation(o, brokerFeeCalculationType));
        }

        private void CreatePriceInformation(Order order, BrokerFeeCalculationType brokerFeeCalculationType)
        {
            _logger.LogInformation("Create price rows for Order: {orderId}, Customer: {Name}",
                order?.OrderId, order?.CustomerOrganisation?.Name);
            var rankingId = order.Requests.Single(r => r.IsToBeProcessedByBroker || r.IsAcceptedOrApproved).RankingId;
            var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(SelectCompetenceLevelForPriceEstimation(order.CompetenceRequirements?.Select(item => item.CompetenceLevel)));
            var priceInformation = _priceCalculationService.GetPrices(
                order.StartAt,
                order.Duration,
                competenceLevel,
                order.CustomerOrganisation.PriceListType,
                order.CreatedAt,
                _priceCalculationService.GetCalculatedBrokerFee(order, _clock.SwedenNow, brokerFeeCalculationType, competenceLevel, rankingId, order.InterpreterLocations.OrderBy(l => l.Rank).First().InterpreterLocation));
            order.PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, OrderPriceRow>(row)));
        }

        private async Task HandleExpiredRequests()
        {
            var expiredRequestIds = await _tolkDbContext.Requests.ExpiredRequests(_clock.SwedenNow).Select(r => r.RequestId).ToListAsync();

            _logger.LogInformation("Found {count} expired requests to process: {expiredRequestIds}",
                expiredRequestIds.Count, string.Join(", ", expiredRequestIds));

            foreach (var requestId in expiredRequestIds)
            {
                using var trn = await _tolkDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var expiredRequest = await _tolkDbContext.Requests.GetExpiredRequest(_clock.SwedenNow, requestId);
                    if (expiredRequest == null)
                    {
                        _logger.LogInformation("Request {requestId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                            requestId);
                    }
                    else
                    {
                        _logger.LogInformation("Processing expired request {requestId} for Order {orderId}.",
                            expiredRequest.RequestId, expiredRequest.OrderId);

                        if (expiredRequest.Order.StartAt <= _clock.SwedenNow)
                        {
                            if (expiredRequest.Status == RequestStatus.AwaitingDeadlineFromCustomer)
                            {
                                expiredRequest.Status = RequestStatus.NoDeadlineFromCustomer;
                                expiredRequest.Order.Status = OrderStatus.NoDeadlineFromCustomer;
                            }
                            else
                            {
                                expiredRequest.Status = RequestStatus.DeniedByTimeLimit;
                                _notificationService.RequestExpiredDueToInactivity(expiredRequest);
                                expiredRequest.Order.Status = OrderStatus.NoBrokerAcceptedOrder;
                            }
                            await TerminateOrder(expiredRequest.Order);
                        }
                        else if (expiredRequest.LatestAnswerTimeForCustomer.HasValue && expiredRequest.LatestAnswerTimeForCustomer <= _clock.SwedenNow)
                        {
                            _notificationService.RequestExpiredDueToNoAnswerFromCustomer(expiredRequest);
                            //always terminate order if customer not answers within LatestAnswerTime set by broker 
                            expiredRequest.Status = RequestStatus.ResponseNotAnsweredByCreator;
                            expiredRequest.Order.Status = OrderStatus.ResponseNotAnsweredByCreator;
                            await TerminateOrder(expiredRequest.Order);
                        }
                        else
                        {
                            bool notFullyAnswered = expiredRequest.AcceptedAt.HasValue && expiredRequest.Status == RequestStatus.AcceptedAwaitingInterpreter;
                            expiredRequest.Status = RequestStatus.DeniedByTimeLimit;
                            if (notFullyAnswered)
                            {
                                _notificationService.RequestExpiredDueToNotFullyAnswered(expiredRequest);
                            }
                            else
                            {
                                _notificationService.RequestExpiredDueToInactivity(expiredRequest);
                            }
                            await CreateRequest(expiredRequest.Order, expiredRequest);
                        }
                        await trn.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing expired request {requestId}", requestId);
                    await trn.RollbackAsync();
                    await SendErrorMail(nameof(HandleExpiredRequests), ex);
                }
            }
        }

        private async Task HandleExpiredRequestGroups()
        {
            var expiredRequestGroupIds = await _tolkDbContext.RequestGroups.ExpiredRequestGroups(_clock.SwedenNow)
                .Select(r => r.RequestGroupId)
                .ToListAsync();

            _logger.LogInformation("Found {count} expired request groups to process: {expiredRequestGroupIds}",
                expiredRequestGroupIds.Count, string.Join(", ", expiredRequestGroupIds));
            foreach (var requestGroupId in expiredRequestGroupIds)
            {
                using var trn = await _tolkDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var expiredRequestGroup = await _tolkDbContext.RequestGroups.GetExpiredRequestGroup(_clock.SwedenNow, requestGroupId);
                    if (expiredRequestGroup == null)
                    {
                        _logger.LogInformation("Request group {requestGroupId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                            requestGroupId);
                    }
                    else
                    {
                        expiredRequestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(expiredRequestGroup.RequestGroupId).ToListAsync();
                        expiredRequestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(expiredRequestGroup.OrderGroupId).ToListAsync();
                        _logger.LogInformation("Processing expired request group {requestGroupId} for Order group {orderGroupId}.",
                            expiredRequestGroup.RequestGroupId, expiredRequestGroup.OrderGroupId);
                        if (expiredRequestGroup.OrderGroup.ClosestStartAt <= _clock.SwedenNow)
                        {
                            expiredRequestGroup.SetStatus(RequestStatus.NoDeadlineFromCustomer);
                            expiredRequestGroup.OrderGroup.SetStatus(OrderStatus.NoDeadlineFromCustomer);
                            await TerminateOrderGroup(expiredRequestGroup.OrderGroup);
                        }
                        else if (expiredRequestGroup.LatestAnswerTimeForCustomer.HasValue && expiredRequestGroup.LatestAnswerTimeForCustomer <= _clock.SwedenNow)
                        {
                            _notificationService.RequestGroupExpiredDueToNoAnswerFromCustomer(expiredRequestGroup);
                            expiredRequestGroup.SetStatus(RequestStatus.ResponseNotAnsweredByCreator);
                            expiredRequestGroup.OrderGroup.SetStatus(OrderStatus.ResponseNotAnsweredByCreator);
                            await TerminateOrderGroup(expiredRequestGroup.OrderGroup);
                        }
                        else
                        {
                            expiredRequestGroup.OrderGroup.RequestGroups = await _tolkDbContext.RequestGroups.GetRequestGroupsForOrderGroup(expiredRequestGroup.OrderGroupId).ToListAsync();
                            bool notFullyAnswered = expiredRequestGroup.AcceptedAt.HasValue && expiredRequestGroup.Status == RequestStatus.AcceptedAwaitingInterpreter;
                            expiredRequestGroup.SetStatus(RequestStatus.DeniedByTimeLimit);
                            if (notFullyAnswered)
                            {
                                _notificationService.RequestGroupExpiredDueToNotFullyAnswered(expiredRequestGroup);
                            }
                            else
                            {
                                _notificationService.RequestGroupExpiredDueToInactivity(expiredRequestGroup);
                            }
                            await CreateRequestGroup(expiredRequestGroup.OrderGroup, expiredRequestGroup);
                        }
                        await trn.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing expired request group {requestGroupId}", requestGroupId);
                    await trn.RollbackAsync();
                    await SendErrorMail(nameof(HandleExpiredRequestGroups), ex);
                }
            }
        }

        private async Task SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendErrorEmail(nameof(OrderService), methodname, ex);
        }

        private async Task HandleExpiredNonAnsweredRespondedRequests()
        {
            var nonAnsweredRespondedRequestsId = await _tolkDbContext.Requests.NonAnsweredRespondedRequests(_clock.SwedenNow).Select(r => r.RequestId).ToListAsync();

            _logger.LogInformation("Found {count} non answered responded requests that expires: {requestIds}",
                nonAnsweredRespondedRequestsId.Count, string.Join(", ", nonAnsweredRespondedRequestsId));

            foreach (var requestId in nonAnsweredRespondedRequestsId)
            {
                using var trn = await _tolkDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var request = await _tolkDbContext.Requests.GetNonAnsweredRespondedRequest(_clock.SwedenNow, requestId);
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
                        _notificationService.RequestExpiredDueToNoAnswerFromCustomer(request);
                        await TerminateOrder(request.Order);
                        await _tolkDbContext.SaveChangesAsync();
                        await trn.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing {methodName} for request {requestId}", nameof(HandleExpiredNonAnsweredRespondedRequests), requestId);
                    await trn.RollbackAsync();
                    await SendErrorMail(nameof(HandleExpiredNonAnsweredRespondedRequests), ex);
                }
            }
        }

        private async Task HandleExpiredNonAnsweredRespondedRequestGroups()
        {
            var nonAnsweredRespondedRequestGroupsId = await _tolkDbContext.RequestGroups.NonAnsweredRespondedRequestGroups(_clock.SwedenNow)
                .Select(rg => rg.RequestGroupId).ToListAsync();

            _logger.LogInformation("Found {count} non answered responded request groups that expires: {nonAnsweredRespondedRequestGroupsId}",
                nonAnsweredRespondedRequestGroupsId.Count, string.Join(", ", nonAnsweredRespondedRequestGroupsId));

            foreach (var requestGroupId in nonAnsweredRespondedRequestGroupsId)
            {
                using var trn = await _tolkDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                try
                {
                    var requestGroup = await _tolkDbContext.RequestGroups.GetNonAnsweredRespondedRequestGroup(_clock.SwedenNow, requestGroupId);
                    if (requestGroup == null)
                    {
                        _logger.LogInformation("Non answered responded request group {requestGroupId} was in list to be processed, but doesn't match criteria when re-read from database - skipping.",
                            requestGroupId);
                    }
                    else
                    {
                        requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
                        requestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
                        _logger.LogInformation("Set new status for non answered responded request group {requestGroupId}.", requestGroupId);
                        requestGroup.SetStatus(RequestStatus.ResponseNotAnsweredByCreator);
                        requestGroup.OrderGroup.SetStatus(OrderStatus.ResponseNotAnsweredByCreator);
                        _notificationService.RequestGroupExpiredDueToNoAnswerFromCustomer(requestGroup);
                        await _tolkDbContext.SaveChangesAsync();
                        await TerminateOrderGroup(requestGroup.OrderGroup);
                        await trn.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure processing {methodName} for request group {requestGroupId}", nameof(HandleExpiredNonAnsweredRespondedRequestGroups), requestGroupId);
                    await trn.RollbackAsync();
                    await SendErrorMail(nameof(HandleExpiredNonAnsweredRespondedRequestGroups), ex);
                }
            }
        }
    }
}
