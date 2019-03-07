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
        private readonly NotificationService _notificationService;
        private readonly OrderService _orderService;
        private readonly RankingService _rankingService;
        private readonly TolkDbContext _tolkDbContext;
        private readonly ISwedishClock _clock;
        private readonly VerificationService _verificationService;

        public RequestService(
            PriceCalculationService priceCalculationService,
            ILogger<RequestService> logger,
            NotificationService notificationService,
            OrderService orderService,
            RankingService rankingService,
            TolkDbContext tolkDbContext,
            ISwedishClock clock,
            VerificationService verificationService
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
            decimal? expectedTravelCosts
        )
        {
            //Get prices
            var prices = _priceCalculationService.GetPrices(request, competenceLevel, expectedTravelCosts);
            VerificationResult? verificationResult = null;
            if (competenceLevel != CompetenceAndSpecialistLevel.OtherInterpreter)
            {
                //Only check if the selected level is other than other.
                verificationResult = await _verificationService.VerifyInterpreter(interpreter.OfficialInterpreterId, request.OrderId, competenceLevel);
            }
            // Acccept the request
            request.Accept(acceptTime, userId, impersonatorId, interpreter, interpreterLocation, competenceLevel, requirementAnswers, attachedFiles, prices, verificationResult);
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
            decimal? expectedTravelCosts
        )
        {
            request.AcceptReplacementOrder(
                acceptTime,
                userId,
                impersonatorId,
                expectedTravelCosts,
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
            decimal? expectedTravelCosts
        )
        {
            Request newRequest = new Request(request.Ranking, request.ExpiresAt, changedAt)
            {
                Order = request.Order,
                Status = RequestStatus.AcceptedNewInterpreterAppointed
            };
            request.Order.Requests.Add(newRequest);
            VerificationResult? verificationResult = null;
            if (competenceLevel != CompetenceAndSpecialistLevel.OtherInterpreter)
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
                request.Order.AllowExceedingTravelCost != AllowExceedingTravelCost.YesShouldBeApproved,
                request,
                verificationResult
            );
            if (request.Status == RequestStatus.Approved && request.Order.AllowExceedingTravelCost != AllowExceedingTravelCost.YesShouldBeApproved)
            {
                _notificationService.RequestChangedInterpreterAccepted(newRequest, InterpereterChangeAcceptOrigin.NoNeedForUserAccept);
            }
            else
            {
                _notificationService.RequestChangedInterpreter(newRequest);
            }
            request.Status = RequestStatus.InterpreterReplaced;
        }

        public async Task SendEmailReminders()
        {
            _logger.LogInformation("Start Sending Reminder Emails");
            List<Request> notAcceptedRequests = await _tolkDbContext.Requests
                .Where(req => req.Status == RequestStatus.Accepted || req.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                 .Include(req => req.Order)
                    .ThenInclude(order => order.CreatedByUser)
                 .Include(req => req.Order)
                    .ThenInclude(order => order.ContactPersonUser)
                .Include(req => req.Ranking)
                    .ThenInclude(rank => rank.Broker)
                .Include(req => req.Interpreter)
                .Where(req => req.Order.StartAt > _clock.SwedenNow)
                .ToListAsync();

            foreach (Request request in notAcceptedRequests)
            {
                _notificationService.RemindUnhandledRequest(request);
            }

            _logger.LogInformation($"{notAcceptedRequests.Count} email reminders sent");
        }

        public async Task DeleteRequestViews()
        {
            _logger.LogInformation("Start checking for RequestViews to delete");
            List<RequestView> nonDeletedRequestViews = await _tolkDbContext.RequestViews
                .Where(rv => rv.ViewedAt.Date < _clock.SwedenNow.Date)
                .ToListAsync();

            if (nonDeletedRequestViews.Any())
            {
                _logger.LogInformation($"{nonDeletedRequestViews.Count} RequestViews deleted");
                _tolkDbContext.RemoveRange(nonDeletedRequestViews);
                _tolkDbContext.SaveChanges();
                return;
            }
            _logger.LogInformation($"No RequestViews to delete");
        }
    }
}