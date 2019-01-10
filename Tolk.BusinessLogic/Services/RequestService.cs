using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public class RequestService
    {
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger<OrderService> _logger;
        private readonly NotificationService _notificationService;
        private readonly OrderService _orderService;
        private readonly RankingService _rankingService;

        public RequestService(
            PriceCalculationService priceCalculationService,
            ILogger<OrderService> logger,
            NotificationService notificationService,
            OrderService orderService,
            RankingService rankingService
            )
        {
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _notificationService = notificationService;
            _orderService = orderService;
            _rankingService = rankingService;
        }

        public void Accept(
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
            // Acccept the request
            request.Accept(acceptTime, userId, impersonatorId, interpreter, interpreterLocation, competenceLevel, requirementAnswers, attachedFiles, prices);
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

        public void ChangeInterpreter(
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
            newRequest.ReplaceInterpreter(changedAt,
                userId,
                impersonatorId,
                interpreter,
                interpreterLocation,
                competenceLevel,
                requirementAnswers,
                attachedFiles.Select(f => new RequestAttachment { AttachmentId = f.AttachmentId }),
                _priceCalculationService.GetPrices(request, competenceLevel, expectedTravelCosts),
                !request.Order.AllowMoreThanTwoHoursTravelTime,
                request
                 );
            if (request.Status == RequestStatus.Approved && !request.Order.AllowMoreThanTwoHoursTravelTime)
            {
                _notificationService.RequestChangedInterpreterAccepted(newRequest, InterpereterChangeAcceptOrigin.NoNeedForUserAccept);
            }
            else
            {
                _notificationService.RequestChangedInterpreter(newRequest);
            }
            request.Status = RequestStatus.InterpreterReplaced;

        }
    }
}