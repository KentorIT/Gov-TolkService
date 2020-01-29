using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class RequestService
    {
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger<RequestService> _logger;
        private readonly INotificationService _notificationService;
        private readonly OrderService _orderService;
        private readonly TolkDbContext _tolkDbContext;
        private readonly ISwedishClock _clock;
        private readonly VerificationService _verificationService;
        private readonly EmailService _emailService;
        private readonly ITolkBaseOptions _tolkBaseOptions;

        public RequestService(
            PriceCalculationService priceCalculationService,
            ILogger<RequestService> logger,
            INotificationService notificationService,
            OrderService orderService,
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            VerificationService verificationService,
             EmailService emailService,
            ITolkBaseOptions tolkBaseOptions
            )
        {
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _notificationService = notificationService;
            _orderService = orderService;
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _verificationService = verificationService;
            _emailService = emailService;
            _tolkBaseOptions = tolkBaseOptions;
        }

        public async Task Accept(
            Request request,
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            InterpreterBroker interpreter,
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            List<RequestAttachment> attachedFiles,
            decimal? expectedTravelCosts,
            string expectedTravelCostInfo,
            DateTimeOffset? latestAnswerTimeForCustomer
        )
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(Accept), nameof(RequestService));
            NullCheckHelper.ArgumentCheckNull(interpreter, nameof(Accept), nameof(RequestService));

            AcceptRequest(request, acceptTime, userId, impersonatorId, interpreter, interpreterLocation, competenceLevel, requirementAnswers, attachedFiles, expectedTravelCosts, expectedTravelCostInfo, await VerifyInterpreter(request.OrderId, interpreter, competenceLevel), latestAnswerTimeForCustomer: latestAnswerTimeForCustomer);
            //Create notification
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    _notificationService.RequestAccepted(request);
                    break;
                case RequestStatus.Approved:
                    _notificationService.RequestAnswerAutomaticallyApproved(request);
                    break;
                default:
                    throw new NotImplementedException("NOT OK!!");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Extra interpreter answer is only required if the request group contains a request for an extra interpreter.")]
        public async Task AcceptGroup(
            RequestGroup requestGroup,
            DateTimeOffset answerTime,
            int userId,
            int? impersonatorId,
            InterpreterLocation interpreterLocation,
            InterpreterAnswerDto interpreter,
            InterpreterAnswerDto extraInterpreter,
            List<RequestGroupAttachment> attachedFiles,
            DateTimeOffset? latestAnswerTimeForCustomer
        )
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(AcceptGroup), nameof(RequestService));
            NullCheckHelper.ArgumentCheckNull(interpreter, nameof(AcceptGroup), nameof(RequestService));
            if (requestGroup.HasExtraInterpreter)
            {
                NullCheckHelper.ArgumentCheckNull(extraInterpreter, nameof(AcceptGroup), nameof(RequestService));
            }
            var declinedRequests = new List<Request>();

            bool isSingleOccasion = requestGroup.OrderGroup.IsSingleOccasion;
            bool hasExtraInterpreter = requestGroup.HasExtraInterpreter;
            ValidateInterpreters(interpreter, extraInterpreter, hasExtraInterpreter);

            //TODO check if travelcost > 0 when AllowExceedingTravelCost == No or if InterpreterLocation is Phone or Video
            bool hasTravelCosts = (interpreter.ExpectedTravelCosts ?? 0) > 0 || (extraInterpreter?.ExpectedTravelCosts ?? 0) > 0;
            var travelCostsShouldBeApproved = hasTravelCosts && requestGroup.OrderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved;
            bool partialAnswer = false;
            //1. Get the verification results for the interpreter(s)
            var verificationResult = await VerifyInterpreter(requestGroup.OrderGroup.FirstOrder.OrderId, interpreter.Interpreter, interpreter.CompetenceLevel);
            var extraInterpreterVerificationResult = hasExtraInterpreter && extraInterpreter.Accepted ?
                await VerifyInterpreter(requestGroup.OrderGroup.FirstOrder.OrderId, extraInterpreter.Interpreter, extraInterpreter.CompetenceLevel) :
                null;
            foreach (var request in requestGroup.Requests)
            {
                bool isExtraInterpreterOccasion = request.Order.IsExtraInterpreterForOrderId.HasValue;
                if (isExtraInterpreterOccasion)
                {
                    if (extraInterpreter.Accepted)
                    {
                        AcceptReqestGroupRequest(request,
                            answerTime,
                            userId,
                            impersonatorId,
                            extraInterpreter,
                            interpreterLocation,
                            Enumerable.Empty<RequestAttachment>().ToList(),
                            extraInterpreterVerificationResult,
                            latestAnswerTimeForCustomer,
                            travelCostsShouldBeApproved       
                       );
                    }
                    else
                    {
                        partialAnswer = true;
                        await Decline(request, answerTime, userId, impersonatorId, extraInterpreter.DeclineMessage, false, false);
                        declinedRequests.Add(request);
                    }
                }
                else
                {
                    AcceptReqestGroupRequest(request,
                        answerTime,
                        userId,
                        impersonatorId,
                        interpreter,
                        interpreterLocation,
                        Enumerable.Empty<RequestAttachment>().ToList(),
                        verificationResult,
                        latestAnswerTimeForCustomer,
                        travelCostsShouldBeApproved
                    );
                }
            }

            // add the attachments to the group...
            requestGroup.Accept(answerTime, userId, impersonatorId, attachedFiles, hasTravelCosts, partialAnswer, latestAnswerTimeForCustomer);

            if (partialAnswer)
            {
                //Need to split the declined part of the group, and make a separate order- and request group for that, to forward to the next in line. if any...
                await _orderService.CreatePartialRequestGroup(requestGroup, declinedRequests);
                if (requestGroup.RequiresApproval(hasTravelCosts))
                {
                    _notificationService.PartialRequestGroupAnswerAccepted(requestGroup);
                }
                else
                {
                    _notificationService.PartialRequestGroupAnswerAutomaticallyApproved(requestGroup);
                }
            }
            else
            {
                //2. Set the request group and order group in correct state
                if (requestGroup.RequiresApproval(hasTravelCosts))
                {
                    _notificationService.RequestGroupAccepted(requestGroup);
                }
                else
                {
                    _notificationService.RequestGroupAnswerAutomaticallyApproved(requestGroup);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "To follow the standards when using current service")]
        public void Acknowledge(Request request, DateTimeOffset acknowledgeTime, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(Acknowledge), nameof(RequestService));
            request.Received(acknowledgeTime, userId, impersonatorId);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "To follow the standards when using current service")]
        public void AcknowledgeGroup(RequestGroup requestGroup, DateTimeOffset acknowledgeTime, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(AcknowledgeGroup), nameof(RequestService));
            requestGroup.Received(acknowledgeTime, userId, impersonatorId);
        }

        public void AcceptReplacement(
            Request request,
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            InterpreterLocation interpreterLocation,
            decimal? expectedTravelCosts,
            string expectedTravelCostInfo
        )
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(AcceptReplacement), nameof(RequestService));
            request.AcceptReplacementOrder(
                acceptTime,
                userId,
                impersonatorId,
                expectedTravelCostInfo,
                interpreterLocation,
                _priceCalculationService.GetPrices(request, (CompetenceAndSpecialistLevel)request.CompetenceLevel, expectedTravelCosts)
            );
            _notificationService.RequestReplamentOrderAccepted(request);
        }

        public async Task Decline(
            Request request,
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message,
            bool notify = true,
            bool createNewRequest = true)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(Decline), nameof(RequestService));
            request.Decline(declinedAt, userId, impersonatorId, message);
            if (!request.Order.ReplacingOrderId.HasValue)
            {
                if (createNewRequest)
                {
                    await _orderService.CreateRequest(request.Order, request, notify: notify);
                }
                if (notify)
                {
                    _notificationService.RequestDeclinedByBroker(request);
                }
            }
            else
            {
                if (notify)
                {
                    _notificationService.RequestReplamentOrderDeclinedByBroker(request);
                }
            }
        }

        public async Task DeclineGroup(
            RequestGroup requestGroup,
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(DeclineGroup), nameof(RequestService));
            requestGroup.Decline(declinedAt, userId, impersonatorId, message);
            await _orderService.CreateRequestGroup(requestGroup.OrderGroup, requestGroup);
            _notificationService.RequestGroupDeclinedByBroker(requestGroup);
        }

        public void CancelByBroker(Request request, DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(CancelByBroker), nameof(RequestService));
            request.CancelByBroker(cancelledAt, userId, impersonatorId, message);
            _notificationService.RequestCancelledByBroker(request);
        }

        public async Task ChangeInterpreter(
            Request request,
            DateTimeOffset changedAt,
            int userId,
            int? impersonatorId,
            InterpreterBroker interpreter,
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            IEnumerable<RequestAttachment> attachedFiles,
            decimal? expectedTravelCosts,
            string expectedTravelCostInfo,
            DateTimeOffset? latestAnswerTimeForCustomer
        )
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ChangeInterpreter), nameof(RequestService));
            NullCheckHelper.ArgumentCheckNull(interpreter, nameof(ChangeInterpreter), nameof(RequestService));
            if (interpreter.InterpreterBrokerId == GetOtherInterpreterIdForSameOccasion(request) && !(interpreter.Interpreter?.IsProtected ?? false))
            {
                throw new InvalidOperationException("Det går inte att tillsätta samma tolk som redan är tillsatt som extra tolk för samma tillfälle.");
            }
            Request newRequest = new Request(request.Ranking, request.ExpiresAt, changedAt, isChangeInterpreter: true, requestGroup: request.RequestGroup)
            {
                Order = request.Order,
                Status = RequestStatus.AcceptedNewInterpreterAppointed
            };
            bool noNeedForUserAccept = NoNeedForUserAccept(request, expectedTravelCosts);
            request.Order.Requests.Add(newRequest);
            VerificationResult? verificationResult = null;
            if (competenceLevel != CompetenceAndSpecialistLevel.OtherInterpreter && _tolkBaseOptions.Tellus.IsActivated)
            {
                //Only check if the selected level is other than other.
                verificationResult = await _verificationService.VerifyInterpreter(interpreter.OfficialInterpreterId, request.OrderId, competenceLevel);
            }

            newRequest.ReplaceInterpreter(changedAt,
                userId,
                impersonatorId,
                interpreter,
                interpreterLocation,
                competenceLevel,
                requirementAnswers,
                attachedFiles.Select(f => new RequestAttachment { AttachmentId = f.AttachmentId }),
                _priceCalculationService.GetPrices(request, competenceLevel, expectedTravelCosts),
                noNeedForUserAccept,
                request,
                expectedTravelCostInfo,
                verificationResult,
                latestAnswerTimeForCustomer
            );
            // need requestid for the link
            await _tolkDbContext.SaveChangesAsync();
            if (noNeedForUserAccept)
            {
                _notificationService.RequestChangedInterpreterAccepted(newRequest, InterpereterChangeAcceptOrigin.NoNeedForUserAccept);
            }
            else
            {
                _notificationService.RequestChangedInterpreter(newRequest);
            }
            request.Status = RequestStatus.InterpreterReplaced;
        }

        public async Task ConfirmDenial(
            Request request,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmDenial), nameof(RequestService));
            request.ConfirmDenial(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmGroupDenial(
            RequestGroup requestGroup,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(ConfirmGroupDenial), nameof(RequestService));
            requestGroup.ConfirmDenial(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmNoAnswer(
            Request request,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmNoAnswer), nameof(RequestService));
            request.ConfirmNoAnswer(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmGroupNoAnswer(
            RequestGroup requestGroup,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(ConfirmGroupNoAnswer), nameof(RequestService));
            requestGroup.ConfirmNoAnswer(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmNoRequisition(
            Request request,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmNoAnswer), nameof(RequestService));
            request.ConfirmNoRequisition(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmCancellation(
            Request request,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmCancellation), nameof(RequestService));
            request.ConfirmCancellation(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task SendEmailReminders()
        {
            _logger.LogInformation("Start Sending Reminder Emails");
            List<Request> notAcceptedRequests = await _tolkDbContext.Requests
                 .Include(req => req.Order)
                    .ThenInclude(order => order.CreatedByUser)
                 .Include(req => req.Order)
                    .ThenInclude(order => order.ContactPersonUser)
                .Include(req => req.Ranking)
                    .ThenInclude(rank => rank.Broker)
                .Include(req => req.Order)
                    .ThenInclude(order => order.CustomerUnit)
                .Include(req => req.Interpreter)
                .Where(req => req.Order.StartAt > _clock.SwedenNow &&
                    (req.Status == RequestStatus.Accepted || req.Status == RequestStatus.AcceptedNewInterpreterAppointed))
                .ToListAsync();

            foreach (Request request in notAcceptedRequests)
            {
                _notificationService.RemindUnhandledRequest(request);
            }

            _logger.LogInformation($"{notAcceptedRequests.Count} email reminders sent");
        }

        /// <summary>
        /// Deletes RequestViews that remain in database if session ends for user (session is set to 120 min)
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task DeleteRequestViews()
        {
            _logger.LogInformation("Start checking for RequestViews to delete");
            List<RequestView> requestViewsToDelete = await _tolkDbContext.RequestViews
                .Where(rv => rv.ViewedAt < _clock.SwedenNow.AddMinutes(-121)).ToListAsync();

            if (requestViewsToDelete.Any())
            {
                try
                {
                    _tolkDbContext.RemoveRange(requestViewsToDelete);
                    await _tolkDbContext.SaveChangesAsync();
                    _logger.LogInformation($"{requestViewsToDelete.Count} RequestViews deleted");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failing {methodName}", nameof(DeleteRequestViews));
                    await SendErrorMail(nameof(DeleteRequestViews), ex);
                }
            }
            else
            {
                _logger.LogInformation($"No RequestViews to delete");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task DeleteRequestGroupViews()
        {
            _logger.LogInformation("Start checking for RequestGroupViews to delete");
            List<RequestGroupView> viewsToDelete = await _tolkDbContext.RequestGroupViews
                .Where(rv => rv.ViewedAt < _clock.SwedenNow.AddMinutes(-121)).ToListAsync();

            if (viewsToDelete.Any())
            {
                try
                {
                    _tolkDbContext.RemoveRange(viewsToDelete);
                    await _tolkDbContext.SaveChangesAsync();
                    _logger.LogInformation($"{viewsToDelete.Count} RequestGroupViews deleted");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failing {methodName}", nameof(DeleteRequestGroupViews));
                    await SendErrorMail(nameof(DeleteRequestGroupViews), ex);
                }
            }
            else
            {
                _logger.LogInformation($"No RequestGroupViews to delete");
            }
        }

        private void ValidateInterpreters(InterpreterAnswerDto interpreter, InterpreterAnswerDto extraInterpreter, bool hasExtraInterpreter)
        {
            if (!interpreter.Accepted && !hasExtraInterpreter)
            {
                throw new InvalidOperationException("Ingen tolk är tillsatt. använd \"Tacka nej till bokning\" om intentionen är att inte tillsätta någon tolk.");
            }

            if (!interpreter.Accepted && hasExtraInterpreter && extraInterpreter.Accepted)
            {
                throw new InvalidOperationException("Om den sammanhållna bokningen beställer två tolkar så måste den första tolken tillsättas.");
            }
            if (!interpreter.Accepted && hasExtraInterpreter && !extraInterpreter.Accepted)
            {
                throw new InvalidOperationException("Ingen tolk är tillsatt. använd \"Tacka nej till bokning\" om intentionen är att inte tillsätta någon tolk.");
            }

            if (hasExtraInterpreter && !extraInterpreter.Accepted && !_tolkBaseOptions.AllowDeclineExtraInterpreterOnRequestGroups)
            {
                throw new InvalidOperationException("Om den sammanhållna bokningen beställer två tolkar så måste den extra tolken tillsättas.");
            }
            if (hasExtraInterpreter && interpreter.Accepted && extraInterpreter.Accepted && interpreter.Interpreter.InterpreterBrokerId == extraInterpreter.Interpreter.InterpreterBrokerId && !(interpreter.Interpreter.Interpreter?.IsProtected ?? false))
            {
                throw new InvalidOperationException("Man kan inte tillsätta samma tolk som extra tolk.");
            }
        }

        private void AcceptRequest(Request request, DateTimeOffset acceptTime, int userId, int? impersonatorId, InterpreterBroker interpreter, InterpreterLocation interpreterLocation, CompetenceAndSpecialistLevel competenceLevel, List<OrderRequirementRequestAnswer> requirementAnswers, List<RequestAttachment> attachedFiles, decimal? expectedTravelCosts, string expectedTravelCostInfo, VerificationResult? verificationResult, DateTimeOffset? latestAnswerTimeForCustomer, bool overrideRequireAccept = false)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(AcceptRequest), nameof(RequestService));
            //Get prices
            var prices = _priceCalculationService.GetPrices(request, competenceLevel, expectedTravelCosts);
            // Acccept the request
            request.Accept(acceptTime, userId, impersonatorId, interpreter, interpreterLocation, competenceLevel, requirementAnswers, attachedFiles, prices, expectedTravelCostInfo, latestAnswerTimeForCustomer, verificationResult, overrideRequireAccept);
        }

        private void AcceptReqestGroupRequest(Request request, DateTimeOffset acceptTime, int userId, int? impersonatorId, InterpreterAnswerDto interpreter, InterpreterLocation interpreterLocation, List<RequestAttachment> attachedFiles, VerificationResult? verificationResult, DateTimeOffset? latestAnswerTimeForCustomer, bool overrideRequireAccept = false)
        {
            AcceptRequest(request, acceptTime, userId, impersonatorId, interpreter.Interpreter, interpreterLocation, interpreter.CompetenceLevel, ReplaceIds(request.Order.Requirements, interpreter.RequirementAnswers).ToList(), attachedFiles, interpreter.ExpectedTravelCosts, interpreter.ExpectedTravelCostInfo, verificationResult, latestAnswerTimeForCustomer, overrideRequireAccept);
        }

        private static IEnumerable<OrderRequirementRequestAnswer> ReplaceIds(List<OrderRequirement> requirements, IEnumerable<OrderRequirementRequestAnswer> requirementAnswers)
        {
            foreach (var answer in requirementAnswers)
            {
                yield return new OrderRequirementRequestAnswer
                {
                    OrderRequirementId = requirements.Single(r => r.OrderGroupRequirementId == answer.OrderRequirementId).OrderRequirementId,
                    Answer = answer.Answer,
                    CanSatisfyRequirement = answer.CanSatisfyRequirement,
                };
            }
        }

        private async Task<VerificationResult?> VerifyInterpreter(int orderId, InterpreterBroker interpreter, CompetenceAndSpecialistLevel competenceLevel)
        {
            VerificationResult? verificationResult = null;
            if (competenceLevel != CompetenceAndSpecialistLevel.OtherInterpreter && _tolkBaseOptions.Tellus.IsActivated)
            {
                //Only check if the selected level is other than other.
                verificationResult = await _verificationService.VerifyInterpreter(interpreter.OfficialInterpreterId, orderId, competenceLevel);
            }

            return verificationResult;
        }

        public int? GetOtherInterpreterIdForSameOccasion(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(GetOtherInterpreterIdForSameOccasion), nameof(RequestService));
            return request.Order.IsExtraInterpreterForOrder != null ? request.Order.IsExtraInterpreterForOrder.Requests.Where(r => r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed || r.Status == RequestStatus.Approved).SingleOrDefault()?.InterpreterBrokerId :
                 request.Order.ExtraInterpreterOrder?.Requests.Where(r => r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed || r.Status == RequestStatus.Approved).SingleOrDefault()?.InterpreterBrokerId;
        }

        private static bool NoNeedForUserAccept(Request request, decimal? expectedTravelCosts)
        {
            if (!expectedTravelCosts.HasValue || request.Order.AllowExceedingTravelCost != AllowExceedingTravelCost.YesShouldBeApproved)
            {
                return true;
            }
            decimal largestApprovedAmount = request.Order.Requests
                .Where(req => (req.Status == RequestStatus.Approved || req.Status == RequestStatus.InterpreterReplaced) && req.AnswerProcessedAt.HasValue)
                .Select(r => r.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.TravelCost).Sum(pr => pr.Price) as decimal?)
                .Max() ?? 0;
            return largestApprovedAmount >= expectedTravelCosts.Value;
        }

        private async Task SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendErrorEmail(nameof(RequestService), methodname, ex);
        }
    }
}