using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class NotificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISwedishClock _clock;
        private readonly PriceCalculationService _priceCalculationService;

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock,
            PriceCalculationService priceCalculationService
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _priceCalculationService = priceCalculationService;
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

        public void OrderContactPersonChanged(int orderId)
        {
            //get order again to get user for new contact (if any, both current contact and previous contact can be null)
            Order order = _dbContext.Orders
                .Include(o => o.ContactPersonUser)
                .Include(o => o.OrderContactPersonHistory).ThenInclude(cph => cph.PreviousContactPersonUser)
                .Single(o => o.OrderId == orderId);
            AspNetUser previousContactUser = order.OrderContactPersonHistory.OrderByDescending(cph => cph.OrderContactPersonHistoryId).First().PreviousContactPersonUser;
            AspNetUser currentContactUser = order.ContactPersonUser;

            string orderNumber = order.OrderNumber;

            string subject = $"Behörighet ändrad för tolkuppdrag avrops-ID {orderNumber}";
            string bodyPreviousContact = $"Behörighet att godkänna eller underkänna rekvisition har ändrats. Du har inte längre denna behörighet för avrop {orderNumber}. \n\nDetta mejl går inte att svara på.";
            string bodyCurrentContact = $"Behörighet att godkänna eller underkänna rekvisition har ändrats. Du har nu behörighet att utföra detta för avrop {orderNumber}.\n\nDetta mejl går inte att svara på.";

            if (!string.IsNullOrEmpty(previousContactUser?.Email))
            {
                _dbContext.Add(new OutboundEmail(previousContactUser.Email, subject, bodyPreviousContact, _clock.SwedenNow));
            }
            else if (previousContactUser != null)
            {
                _logger.LogInformation($"No email sent for ordernumber {orderNumber} on contact person change, no email is set for user PreviousContactUser {previousContactUser.Id}.");
            }
            if (!string.IsNullOrEmpty(currentContactUser?.Email))
            {
                _dbContext.Add(new OutboundEmail(currentContactUser.Email, subject, bodyCurrentContact, _clock.SwedenNow));
            }
            else if (currentContactUser != null)
            {
                _logger.LogInformation($"No email sent for ordernumber {orderNumber} on contact person change, no email is set for user CurrentContactUser {currentContactUser.Id}.");
            }
            _dbContext.SaveChanges();

#warning broker should get an order_information_updated notification
        }

        public void ComplaintCreated(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var receipent = complaint.Request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"En reklamation har registrerats på avrop {orderNumber}",
                    $"Reklamation för avrop {orderNumber} har skapats med följande meddelande:\n{complaint.ComplaintType.GetDescription()}\n{complaint.ComplaintMessage}" +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for Complaint Created for order number {orderNumber}, no email is set for broker.");
            }
        }

        public void ComplaintDisputed(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var receipent = complaint.CreatedByUser.Email;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"Reklamation kopplad till tolkuppdrag {orderNumber} har blivit bestriden",
                    $"Reklamation för avrop {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage}" +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for Complaint Created for order number {orderNumber}, no email is set for user.");
            }
        }

        public void ComplaintDisputePendingTrial(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var receipent = complaint.Request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"Ert bestridande av reklamation avslogs på avrop {orderNumber}",
                    $"Bestridande av reklamation för avrop {orderNumber} har avslagits med följande meddelande:\n{complaint.AnswerDisputedMessage}" +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for Complaint Created for order number {orderNumber}, no email is set for broker.");
            }
        }

        public void ComplaintTerminatedAsDisputeAccepted(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var receipent = complaint.Request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"Ert bestridande av reklamation har godtagits på avrop {orderNumber}",
                    $"Bestridande av reklamation för avrop {orderNumber} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage}" +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for Complaint Created for order number {orderNumber}, no email is set for broker.");
            }
        }

        public void RequisitionCreated(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var receipent = requisition.Request.Order.CreatedByUser.Email;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"En rekvisition har registrerats på avrop {orderNumber}",
                    $"En rekvisition har registrerats på avrop {orderNumber}" +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for requisition action {requisition.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }

        public void RequisitionApproved(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var receipent = requisition.CreatedByUser.Email;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"Rekvisition för avrop {orderNumber} har godkänts",
                    $"Rekvisition för avrop {orderNumber} har godkänts" +
                        "\n\nKostnader att fakturera:\n\n" + GetRequisitionPriceInformationForMail(requisition) +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for requisition action {requisition.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }

        public void RequisitionDenied(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var receipent = requisition.CreatedByUser.Email;
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    $"Rekvisition för avrop {orderNumber} har underkänts",
                    $"Rekvisition för avrop {orderNumber} har underkänts med följande meddelande:\n{requisition.DenyMessage}" +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for requisition action {requisition.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }

        private string GetRequisitionPriceInformationForMail(Requisition requisition)
        {
            if (requisition.PriceRows == null)
            {
                return string.Empty;
            }
            else
            {
                DisplayPriceInformation priceInfo = _priceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList());
                string invoiceInfo = string.Empty;
                invoiceInfo += $"{priceInfo.HeaderDescription}\n\n";
                foreach (DisplayPriceRow dpr in priceInfo.DisplayPriceRows)
                {
                    invoiceInfo += $"{dpr.Description}:\n{dpr.Price.ToString("#,0.00 SEK")}\n\n";
                }
                invoiceInfo += $"Summa totalt att fakturera: {priceInfo.TotalPrice.ToString("#,0.00 SEK")}";
                return invoiceInfo;
            }
        }
    }
}
