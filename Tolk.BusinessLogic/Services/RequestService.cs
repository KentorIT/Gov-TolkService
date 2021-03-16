using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
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
            //maybe should be moved to AcceptRequest depending on ordergroup requesting each...
            request.Order.Requirements = await _tolkDbContext.OrderRequirements.GetRequirementsForOrder(request.Order.OrderId).ToListAsync();
            request.Order.InterpreterLocations = await _tolkDbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.Order.OrderId).ToListAsync();
            request.Order.CompetenceRequirements = await _tolkDbContext.OrderCompetenceRequirements.GetOrderedCompetenceRequirementsForOrder(request.Order.OrderId).ToListAsync();
            CheckSetLatestAnswerTimeForCustomerValid(latestAnswerTimeForCustomer, nameof(Accept));
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
            CheckSetLatestAnswerTimeForCustomerValid(latestAnswerTimeForCustomer, nameof(AcceptGroup));

            var declinedRequests = new List<Request>();
            await _orderService.AddOrdersWithListsForGroupToProcess(requestGroup.OrderGroup);
            bool isSingleOccasion = requestGroup.OrderGroup.IsSingleOccasion;
            bool hasExtraInterpreter = requestGroup.HasExtraInterpreter;
            if (hasExtraInterpreter)
            {
                NullCheckHelper.ArgumentCheckNull(extraInterpreter, nameof(AcceptGroup), nameof(RequestService));
            }
            ValidateInterpreters(interpreter, extraInterpreter, hasExtraInterpreter);

            bool hasTravelCosts = (interpreter.ExpectedTravelCosts ?? 0) > 0 || (extraInterpreter?.ExpectedTravelCosts ?? 0) > 0;
            var travelCostsShouldBeApproved = hasTravelCosts && requestGroup.OrderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved;
            bool partialAnswer = false;

            //1. Get the verification results for the interpreter(s)
            var verificationResult = await VerifyInterpreter(requestGroup.OrderGroup.FirstOrder.OrderId, interpreter.Interpreter, interpreter.CompetenceLevel);
            var extraInterpreterVerificationResult = hasExtraInterpreter && extraInterpreter.Accepted ?
                await VerifyInterpreter(requestGroup.OrderGroup.FirstOrder.OrderId, extraInterpreter.Interpreter, extraInterpreter.CompetenceLevel) :
                null;
            requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            await AddPriceRowsToRequestsInGroup(requestGroup);
            await AddRequirementAnswersToRequestsInGroup(requestGroup);
            requestGroup.Requests.ForEach(r => r.Order = requestGroup.OrderGroup.Orders.Where(o => o.OrderId == r.OrderId).SingleOrDefault());
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

        public void Acknowledge(Request request, DateTimeOffset acknowledgeTime, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(Acknowledge), nameof(RequestService));
            request.Received(acknowledgeTime, userId, impersonatorId);
        }

        public async Task AcknowledgeGroup(RequestGroup requestGroup, DateTimeOffset acknowledgeTime, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(AcknowledgeGroup), nameof(RequestService));
            requestGroup.Requests ??= await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.Received(acknowledgeTime, userId, impersonatorId);
        }

        public async Task AcceptReplacement(
            Request request,
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            InterpreterLocation interpreterLocation,
            decimal? expectedTravelCosts,
            string expectedTravelCostInfo,
            DateTimeOffset? latestAnswerTimeForCustomer
        )
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(AcceptReplacement), nameof(RequestService));
            CheckSetLatestAnswerTimeForCustomerValid(latestAnswerTimeForCustomer, nameof(AcceptReplacement));
            request.Order.InterpreterLocations = await _tolkDbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.Order.OrderId).ToListAsync();
            request.AcceptReplacementOrder(
                acceptTime,
                userId,
                impersonatorId,
                expectedTravelCostInfo,
                interpreterLocation,
                _priceCalculationService.GetPrices(request, (CompetenceAndSpecialistLevel)request.CompetenceLevel, expectedTravelCosts),
                latestAnswerTimeForCustomer
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
            requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.OrderGroup.Orders = await _tolkDbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            requestGroup.OrderGroup.RequestGroups = await _tolkDbContext.RequestGroups.GetRequestGroupsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
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
            CheckSetLatestAnswerTimeForCustomerValid(latestAnswerTimeForCustomer, nameof(ChangeInterpreter));
            request.Order.Requirements = await _tolkDbContext.OrderRequirements.GetRequirementsForOrder(request.Order.OrderId).ToListAsync();
            request.Order.InterpreterLocations = await _tolkDbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.Order.OrderId).ToListAsync();
            request.Order.CompetenceRequirements = await _tolkDbContext.OrderCompetenceRequirements.GetOrderedCompetenceRequirementsForOrder(request.Order.OrderId).ToListAsync();
            if (interpreter.InterpreterBrokerId == await GetOtherInterpreterIdForSameOccasion(request) && !(interpreter.Interpreter?.IsProtected ?? false))
            {
                throw new InvalidOperationException("Det går inte att tillsätta samma tolk som redan är tillsatt som extra tolk för samma tillfälle.");
            }

            Request newRequest = new Request(request.Ranking, request.ExpiresAt, changedAt, isChangeInterpreter: true, requestGroup: request.RequestGroup)
            {
                Order = request.Order,
                Status = RequestStatus.AcceptedNewInterpreterAppointed
            };
            bool noNeedForUserAccept = await NoNeedForUserAccept(request, expectedTravelCosts);
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
                attachedFiles,
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

        public async Task ConfirmGroupCancellation(
            RequestGroup requestGroup,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(ConfirmGroupCancellation), nameof(RequestService));
            requestGroup.ConfirmCancellation(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmNoRequisition(
            Request request,
            DateTimeOffset confirmedAt,
            int userId,
            int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmNoRequisition), nameof(RequestService));
            request.ConfirmNoRequisition(confirmedAt, userId, impersonatorId);
            await _tolkDbContext.SaveChangesAsync();
        }

        public async Task ConfirmOrderChange(
           Request request,
           List<int> confirmedOrderChangeLogEntriesId,
           DateTimeOffset confirmedAt,
           int userId,
           int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(ConfirmOrderChange), nameof(RequestService));
            request.ConfirmOrderChange(confirmedOrderChangeLogEntriesId, confirmedAt, userId, impersonatorId);
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

        public async Task SendEmailRemindersNonApprovedRequests()
        {
            _logger.LogInformation("Start sending reminder emails for non answered responded rquests");
            var notApprovedRequests = await _tolkDbContext.Requests.NonAnsweredRespondedRequestsToBeReminded(_clock.SwedenNow).ToListAsync();
            foreach (Request request in notApprovedRequests)
            {
                _notificationService.RemindUnhandledRequest(request);
            }
            _logger.LogInformation($"{notApprovedRequests.Count} email reminders sent for non answered responded rquests");
        }

        public async Task SendEmailRemindersNonApprovedRequestGroups()
        {
            _logger.LogInformation("Start sending reminder emails for non answered responded rquestgroups");
            var notApprovedRequestGroups = await _tolkDbContext.RequestGroups.NonAnsweredRespondedRequestGroupsToBeReminded(_clock.SwedenNow).ToListAsync();
            foreach (RequestGroup requestGroup in notApprovedRequestGroups)
            {
                _notificationService.RemindUnhandledRequestGroup(requestGroup);
            }
            _logger.LogInformation($"{notApprovedRequestGroups.Count} email reminders sent for non answered responded rquestgroups");
        }

        /// <summary>
        /// Deletes RequestViews that remain in database if session ends for user (session is set to 120 min)
        /// </summary>
        /// <returns></returns>
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

        private void CheckSetLatestAnswerTimeForCustomerValid(DateTimeOffset? latestAnswerTimeForCustomer, string methodName)
        {
            if (!_tolkBaseOptions.EnableSetLatestAnswerTimeForCustomer && latestAnswerTimeForCustomer != null)
            {
                _logger.LogError("SetLatestAnswerTimeForCustomer in not enabled but has a value {methodName}", methodName);
                throw new InvalidOperationException("SetLatestAnswerTimeForCustomer in not enabled but has a value!");
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

        public async Task<int?> GetOtherInterpreterIdForSameOccasion(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(GetOtherInterpreterIdForSameOccasion), nameof(RequestService));
            List<Request> requests = null;
            if (request.Order.IsExtraInterpreterForOrder != null)
            {
                requests = await _tolkDbContext.Requests.GetRequestsForOrder(request.Order.IsExtraInterpreterForOrder.OrderId).ToListAsync();
            }
            else if (request.Order.ExtraInterpreterOrder != null)
            {
                requests = await _tolkDbContext.Requests.GetRequestsForOrder(request.Order.ExtraInterpreterOrder.OrderId).ToListAsync();
            }
            return requests?.Where(r => r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed || r.Status == RequestStatus.Approved).SingleOrDefault()?.InterpreterBrokerId;
        }

        public async Task<RequestGroup> AddRequestsWithConfirmationListsToRequestGroup(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(AddRequestsWithConfirmationListsToRequestGroup), nameof(RequestService));
            requestGroup.StatusConfirmations = await _tolkDbContext.RequestGroupStatusConfirmations.GetStatusConfirmationsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.Requests = await _tolkDbContext.Requests.GetRequestsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            var requestStatusConfirmations = await _tolkDbContext.RequestStatusConfirmation.GetRequestStatusConfirmationsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.Requests.ForEach(r => r.RequestStatusConfirmations = requestStatusConfirmations.Where(rsc => rsc.RequestId == r.RequestId).ToList());
            return requestGroup;
        }

        private async Task<RequestGroup> AddPriceRowsToRequestsInGroup(RequestGroup requestGroup)
        {
            var priceRows = await _tolkDbContext.RequestPriceRows.GetRequestPriceRowsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.Requests.ForEach(r => r.PriceRows = priceRows.Where(p => p.RequestId == r.RequestId).ToList());
            return requestGroup;
        }

        private async Task<RequestGroup> AddRequirementAnswersToRequestsInGroup(RequestGroup requestGroup)
        {
            var requirementAnswers = await _tolkDbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequestsInGroup(requestGroup.RequestGroupId).ToListAsync();
            requestGroup.Requests.ForEach(r => r.RequirementAnswers = requirementAnswers.Where(ra => ra.RequestId == r.RequestId).ToList());
            return requestGroup;
        }

        public async Task<RequestGroup> AddListsForRequestGroup(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(AddListsForRequestGroup), nameof(RequestService));
            await AddRequestsWithConfirmationListsToRequestGroup(requestGroup);
            await AddPriceRowsToRequestsInGroup(requestGroup);
            await AddRequirementAnswersToRequestsInGroup(requestGroup);
            return requestGroup;
        }

        private async Task<bool> NoNeedForUserAccept(Request request, decimal? expectedTravelCosts)
        {
            if (!expectedTravelCosts.HasValue || expectedTravelCosts == 0 || request.Order.AllowExceedingTravelCost != AllowExceedingTravelCost.YesShouldBeApproved)
            {
                return true;
            }
            var requests = await _tolkDbContext.Requests.GetRequestsForOrder(request.OrderId).ToListAsync();
            var requestsToCheck = requests.Where(req => (req.Status == RequestStatus.Approved || req.Status == RequestStatus.InterpreterReplaced)
                && req.AnswerProcessedAt.HasValue && req.RankingId == request.RankingId).ToList();
            if (!requestsToCheck.Any())
            {
                return false;
            }
            List<decimal?> travelcosts = new List<decimal?>();
            requestsToCheck.ForEach(r => travelcosts.Add(_tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(r.RequestId).ToList().Where(pr => pr.PriceRowType == PriceRowType.TravelCost).Sum(pr => pr.Price)));

            return (travelcosts.Max() ?? 0) >= expectedTravelCosts;
        }
        private async Task SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendErrorEmail(nameof(RequestService), methodname, ex);
        }
    }
}