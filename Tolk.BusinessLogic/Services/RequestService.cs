
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public class RequestService
    {
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger<RequestService> _logger;
        private readonly INotificationService _notificationService;
        private readonly OrderService _orderService;
        private readonly RankingService _rankingService;
        private readonly TolkDbContext _tolkDbContext;
        private readonly ISwedishClock _clock;
        private readonly VerificationService _verificationService;
        private readonly ITolkBaseOptions _tolkBaseOptions;

        public RequestService(
            PriceCalculationService priceCalculationService,
            ILogger<RequestService> logger,
            INotificationService notificationService,
            OrderService orderService,
            RankingService rankingService,
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            VerificationService verificationService,
            ITolkBaseOptions tolkBaseOptions
            )
        {
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _notificationService = notificationService;
            _orderService = orderService;
            _rankingService = rankingService;
            _tolkDbContext = tolkDbContext;
            _clock = clock;
            _verificationService = verificationService;
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
            string expectedTravelCostInfo
        )
        {
            //Get prices
            var prices = _priceCalculationService.GetPrices(request, competenceLevel, expectedTravelCosts);
            VerificationResult? verificationResult = null;
            if (competenceLevel != CompetenceAndSpecialistLevel.OtherInterpreter && _tolkBaseOptions.Tellus.IsActivated)
            {
                //Only check if the selected level is other than other.
                verificationResult = await _verificationService.VerifyInterpreter(interpreter.OfficialInterpreterId, request.OrderId, competenceLevel);
            }
            // Acccept the request
            request.Accept(acceptTime, userId, impersonatorId, interpreter, interpreterLocation, competenceLevel, requirementAnswers, attachedFiles, prices, expectedTravelCostInfo, verificationResult);
            //Create notification
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    _notificationService.RequestAccepted(request);
                    break;
                case RequestStatus.Approved:
                    _notificationService.RequestAnswerAutomaticallyAccepted(request);
                    break;
                default:
                    throw new NotImplementedException("NOT OK!!");
            }
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
            string message)
        {
            request.Decline(declinedAt, userId, impersonatorId, message);
            if (!request.Order.ReplacingOrderId.HasValue)
            {
                await _orderService.CreateRequest(request.Order, request);
                _notificationService.RequestDeclinedByBroker(request);
            }
            else
            {
                _notificationService.RequestReplamentOrderDeclinedByBroker(request);
            }
        }

        public void CancelByBroker(Request request, DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message)
        {
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
            string expectedTravelCostInfo
        )
        {
            Request newRequest = new Request(request.Ranking, request.ExpiresAt, changedAt)
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
                verificationResult
            );
            // needed to be able to get the requestid for the link
            await _tolkDbContext.SaveChangesAsync();
            if (request.Status == RequestStatus.Approved && noNeedForUserAccept)
            {
                _notificationService.RequestChangedInterpreterAccepted(newRequest, InterpereterChangeAcceptOrigin.NoNeedForUserAccept);
            }
            else
            {
                _notificationService.RequestChangedInterpreter(newRequest);
            }
            request.Status = RequestStatus.InterpreterReplaced;
        }

        private bool NoNeedForUserAccept(Request request, decimal? expectedTravelCosts)
        {
            if (!expectedTravelCosts.HasValue || request.Order.AllowExceedingTravelCost != AllowExceedingTravelCost.YesShouldBeApproved)
            {
                return true;
            }
            decimal largestApprovedAmount = request.Order.Requests
                .Where(req => req.Status == RequestStatus.Approved || req.Status == RequestStatus.InterpreterReplaced)
                .Select(r => r.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.TravelCost).Sum(pr => pr.Price) as decimal?)
                .Max() ?? 0;
            return largestApprovedAmount >= expectedTravelCosts.Value;
        }

        public async Task SendEmailReminders()
        {
            _logger.LogInformation("Start Sending Reminder Emails");
            List<Request> notAcceptedRequests = await _tolkDbContext.Requests
                .Where(req => req.IsAccepted)
                 .Include(req => req.Order)
                    .ThenInclude(order => order.CreatedByUser)
                 .Include(req => req.Order)
                    .ThenInclude(order => order.ContactPersonUser)
                .Include(req => req.Ranking)
                    .ThenInclude(rank => rank.Broker)
                .Include(req => req.Order)
                    .ThenInclude(order => order.CustomerUnit)
                .Include(req => req.Interpreter)
                .Where(req => req.Order.StartAt > _clock.SwedenNow)
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
        public async Task DeleteRequestViews()
        {
            _logger.LogInformation("Start checking for RequestViews to delete");
            List<RequestView> requestViewsToDelete = await _tolkDbContext.RequestViews
                .Where(rv => rv.ViewedAt < _clock.SwedenNow.AddMinutes(-121)).ToListAsync();

            if (requestViewsToDelete.Any())
            {
                _logger.LogInformation($"{requestViewsToDelete.Count} RequestViews deleted");
                _tolkDbContext.RemoveRange(requestViewsToDelete);
                _tolkDbContext.SaveChanges();
                return;
            }
            _logger.LogInformation($"No RequestViews to delete");
        }
    }
}