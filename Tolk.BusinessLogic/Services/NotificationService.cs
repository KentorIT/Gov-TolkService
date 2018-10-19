using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class NotificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISwedishClock _clock;

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
        }

        public void OrderReplacementCreated(Order order, Order replacementOrder, Request replamentRequest)
        {
#warning TODO: get order and replament order from request? Or even better get everything from replamentRequest, via navigation properties?
            var brokerEmail = replamentRequest.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(brokerEmail))
            {
                _dbContext.Add(new OutboundEmail(
                    brokerEmail,
                    $"Avrop {order.OrderNumber} har avbokats, med ersättningsuppdrag: {replacementOrder.OrderNumber}",
                    $"\tOrginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tOrginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tErsättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tErsättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tTolk: {replamentRequest.Interpreter.User.FullName}, e-post: {replamentRequest.Interpreter.User.Email}\n" +
                    $"\tSvara senast: {replamentRequest.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}\n\n" +
                    "Detta mejl går inte att svara på.",
                    _clock.SwedenNow));
            }
            else
            {
                _logger.LogInformation("No mail sent to broker {brokerId}, it has no email set.",
                   replamentRequest.Ranking.BrokerId);
            }
        }

        public void RequestAnswerAccepted(Request request)
        {
            //Interpreter part
            string receipent = request.Interpreter.User.Email;
            string orderNumber = request.Order.OrderNumber;
            string body = $"Du har fått ett tolkuppdrag hos {request.Order.CustomerOrganisation.Name} från förmedling {request.Ranking.Broker.Name}. Uppdraget har avrops-ID {orderNumber} och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.";
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"Tilldelat tolkuppdrag avrops-ID {orderNumber}",
                    body +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for orderrequest action {request.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }

            //Broker part
            var brokerEmail = request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(brokerEmail))
            {
                _dbContext.Add(new OutboundEmail(
                    brokerEmail,
                    $"Tolkuppdrag med avrops-ID {orderNumber} verifierat",
                    $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av {request.Interpreter.User.FullName}. \n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent on request answer accepted for order number {request.Order.OrderNumber}, no email is set for broker.");
            }
        }

        public void RequestAnswerDenied(Request request)
        {
            var brokerEmail = request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(brokerEmail))
            {
                _dbContext.Add(new OutboundEmail(
                    brokerEmail,
                    $"Tolkuppdrag med avrops-ID {request.Order.OrderNumber} verifierat",
                    $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av {request.Interpreter.User.FullName}. \n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent on request denied for order number {request.Order.OrderNumber}, no email is set for broker.");
            }
        }

        public void OrderCancelledByCustomer(Request request, bool requestWasApproved, bool createFullCompensationRequisition)
        {
            string orderNumber = request.Order.OrderNumber;
            if (requestWasApproved)
            {
                string interpreter = request.Interpreter?.User.Email;
                if (!string.IsNullOrEmpty(interpreter))
                {
                    _dbContext.Add(new OutboundEmail(
                        interpreter,
                        $"Avbokat avrop avrops-ID {orderNumber}",
                        $"Ditt tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                        $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                        (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.") +
                        "\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                }
                else
                {
                    _logger.LogInformation($"No email sent to interpreter when cancelling {orderNumber}. No email is set for user.");
                }
            }
            string broker = request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(broker))
            {
                if (requestWasApproved)
                {
                    _dbContext.Add(new OutboundEmail(
                        broker,
                        $"Avbokat avrop avrops-ID {orderNumber}",
                        $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                        $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                        (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.") +
                        "\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                }
                else
                {
                    _dbContext.Add(new OutboundEmail(
                        broker,
                        $"Avbokad förfrågan avrops-ID {request.Order.OrderNumber}",
                        $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                        $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                        "\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                }
            }
            else
            {
                _logger.LogInformation($"No email sent to broker when cancelling {orderNumber}. No email is set for broker.");
            }
        }
    }
}
