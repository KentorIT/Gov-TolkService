using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class NotificationService: INotificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISwedishClock _clock;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IMemoryCache _cache;
        private readonly ITolkBaseOptions _tolkBaseOptions;

        private const string brokerSettingsCacheKey = nameof(brokerSettingsCacheKey);

        private const string requireApprovementText = "Observera att ni måste godkänna tillsatt tolk för tolkuppdraget innan bokning kan slutföras eftersom ni har begärt att få förhandsgodkänna resekostnader. Om godkännande inte görs kommer bokningen att annulleras.";

        private static readonly HttpClient client = new HttpClient();

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock,
            PriceCalculationService priceCalculationService,
            IMemoryCache cache,
            ITolkBaseOptions tolkBaseOptions
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _priceCalculationService = priceCalculationService;
            _cache = cache;
            _tolkBaseOptions = tolkBaseOptions;
        }

        public void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition)
        {
            string orderNumber = request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCancelledByCustomer, NotificationChannel.Email);
            if (email != null)
            {
                if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
                {
                    string body = $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                         $"Uppdraget har boknings-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                         (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, i de fall något ersättningsuppdrag inte kan ordnas av kund. Observera att ersättning kan tillkomma för eventuell tidsspillan som tolken skulle behövt ta ut för genomförande av aktuellt uppdrag. Även kostnader avseende resor och boende som ej är avbokningsbara, alternativt avbokningskostnader för resor och boende som avbokats kan tillkomma. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.");
                    CreateEmail(email.ContactInformation, $"Avbokat tolkuppdrag boknings-ID {orderNumber}",
                        body + GotoRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(body) + GotoRequestButton(request.RequestId),
                        true);
                }
                else
                {
                    var body = $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                        $"Uppdraget har boknings-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.";
                    CreateEmail(email.ContactInformation, $"Avbokad förfrågan boknings-ID {orderNumber}",
                        body + GotoRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(body) + GotoRequestButton(request.RequestId),
                        true);
                }
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCancelledByCustomer, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                   new RequestCancelledByCustomerModel
                   {
                       OrderNumber = orderNumber,
                       Message = request.CancelMessage
                   },
                   webhook.ContactInformation,
                   NotificationType.RequestCancelledByCustomer,
                   webhook.RecipientUserId
               );
            }
        }

        public void OrderContactPersonChanged(Order order)
        {
            AspNetUser previousContactUser = order.OrderContactPersonHistory.OrderByDescending(cph => cph.OrderContactPersonHistoryId).First().PreviousContactPersonUser;
            AspNetUser currentContactUser = order.ContactPersonUser;

            string orderNumber = order.OrderNumber;

            string subject = $"Behörighet ändrad för tolkuppdrag boknings-ID {orderNumber}";

            if (!string.IsNullOrEmpty(previousContactUser?.Email))
            {
                string body = $"Behörighet att granska rekvisition har ändrats. Du har inte längre denna behörighet för bokning {orderNumber}.";
                CreateEmail(previousContactUser.Email, subject,
                    body + GotoOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(order.OrderId));
            }
            if (!string.IsNullOrEmpty(currentContactUser?.Email))
            {
                string body = $"Behörighet att granska rekvisition har ändrats. Du har nu behörighet att utföra detta för bokning {orderNumber}.";
                CreateEmail(currentContactUser.Email, subject,
                    body + GotoOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(order.OrderId));
            }
            //Broker
            var request = order.Requests.SingleOrDefault(r => r.IsToBeProcessedByBroker || r.IsAcceptedOrApproved);
            if (request != null)
            {
                //if the contact person is changed on a order with no currently active request, no notification should be sent to broker
                var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestInformationUpdated, NotificationChannel.Email);
                if (email != null)
                {
                    string bodyBroker = $"Person som har rätt att granska rekvisition har ändrats för bokning {orderNumber}.";
                    CreateEmail(email.ContactInformation, $"Bokning {order.OrderNumber} har uppdaterats",
                        bodyBroker + GotoRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(bodyBroker) + GotoRequestButton(request.RequestId),
                        true);
                }
                var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestInformationUpdated, NotificationChannel.Webhook);
                if (webhook != null)
                {
                    CreateWebHookCall(
                       new RequestInformationUpdatedModel
                       {
                           OrderNumber = orderNumber,
                       },
                       webhook.ContactInformation,
                       NotificationType.RequestInformationUpdated,
                       webhook.RecipientUserId
                   );
                }
            }
        }

        public void OrderReplacementCreated(Order order)
        {
            Request oldRequest = order.Requests.SingleOrDefault(r => r.Status == RequestStatus.CancelledByCreator);

            Order replacementOrder = order.ReplacedByOrder;
            Request replacementRequest = replacementOrder.Requests.Single();
            var email = GetBrokerNotificationSettings(replacementRequest.Ranking.BrokerId, NotificationType.RequestReplacementCreated, NotificationChannel.Email);
            if (email != null)
            {
                var bodyPlain = $"\tOrginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tOrginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tErsättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tErsättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tSvara senast: {replacementRequest.ExpiresAt?.ToString("yyyy-MM-dd HH:mm")}\n\n\n" +
                   $"Gå till ersättningsuppdrag: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, replacementRequest.RequestId)}\n" +
                   $"Gå till ursprungligt uppdrag: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, oldRequest.RequestId)}";
                var bodyHtml = $@"
<ul>
<li>Orginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Orginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Svara senast: {replacementRequest.ExpiresAt?.ToString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{GotoRequestButton(replacementRequest.RequestId, textOverride: "Gå till ersättningsuppdrag", autoBreakLines: false)}</div>
<div>{GotoRequestButton(oldRequest.RequestId, textOverride: "Gå till ursprungligt uppdrag", autoBreakLines: false)}</div>";
                CreateEmail(
                     email.ContactInformation,
                     $"Bokning {order.OrderNumber} har avbokats, med ersättningsuppdrag: {replacementOrder.OrderNumber}",
                     bodyPlain,
                     bodyHtml,
                     true);
            }
            var webhook = GetBrokerNotificationSettings(replacementRequest.Ranking.BrokerId, NotificationType.RequestReplacementCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestReplacementCreatedModel
                    {
                        OriginalRequest = GetRequestModel(oldRequest),
                        ReplacementRequest = GetRequestModel(GetRequest(replacementRequest.RequestId))
                    },
                   webhook.ContactInformation,
                   NotificationType.RequestInformationUpdated,
                   webhook.RecipientUserId
               );
            }
        }

        public void OrderNoBrokerAccepted(Order order)
        {
            CreateEmail(GetRecipiantsFromOrder(order),
                $"Bokningsförfrågan {order.OrderNumber} fick ingen tolk",
                $"Ingen förmedling kunde tillsätta en tolk för bokningsförfrågan {order.OrderNumber}. {GotoOrderPlain(order.OrderId)}",
                $"Ingen förmedling kunde tillsätta en tolk för bokningsförfrågan {order.OrderNumber}. {GotoOrderButton(order.OrderId)}"
            );
        }

        public void RequestCreated(Request request)
        {
            var order = request.Order;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Email);
            if (email != null)
            {
                string bodyPlain = $"Bokningsförfrågan för tolkuppdrag {order.OrderNumber} från {order.CustomerOrganisation.Name} har inkommit via Kammarkollegiets avropstjänst för tolkar. Observera att bekräftelse måste lämnas via avropstjänsten.\n" +
                    $"\tUppdragstyp: {EnumHelper.GetDescription(order.AssignentType)}\n" +
                    $"\tRegion: {order.Region.Name}\n" +
                    $"\tSpråk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tStart: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSlut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSvara senast: {request.ExpiresAt?.ToString("yyyy-MM-dd HH:mm")}\n\n\n" +
                    GotoRequestPlain(request.RequestId);
                string bodyHtml = $@"Bokningsförfrågan för tolkuppdrag {order.OrderNumber} från {order.CustomerOrganisation.Name} har inkommit via Kammarkollegiets avropstjänst för tolkar. Observera att bekräftelse måste lämnas via avropstjänsten.<br />
<ul>
<li>Uppdragstyp: {EnumHelper.GetDescription(order.AssignentType)}</li>
<li>Region: {order.Region.Name}</li>
<li>Språk: {order.OtherLanguage ?? order.Language?.Name}</li>
<li>Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Svara senast: {request.ExpiresAt?.ToString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{GotoRequestButton(request.RequestId, textOverride: "Till förfrågan", autoBreakLines: false)}</div>";
                CreateEmail(
                    email.ContactInformation,
                    $"Ny bokningsförfrågan registrerad: {order.OrderNumber}",
                    bodyPlain,
                    bodyHtml,
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    GetRequestModel(request),
                    webhook.ContactInformation,
                    NotificationType.RequestCreated,
                    webhook.RecipientUserId
                );
            }
        }

        public void RequestCreatedWithoutExpiry(Request request)
        {
            string body = $@"Bokningsförfrågan {request.Order.OrderNumber} måste kompletteras med sista svarstid innan den kan skickas till nästa förmedling för tillsättning.

Notera att er förfrågan INTE skickas vidare till nästa förmedling, tills dess sista svarstid är satt.";

            CreateEmail(GetRecipiantsFromOrder(request.Order),
                $"Sista svarstid ej satt på bokningsförfrågan {request.Order.OrderNumber}",
                $"{body} {GotoOrderPlain(request.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GotoOrderButton(request.OrderId)}");

            _logger.LogInformation($"Email created for customer regarding missing expiry on request {request.RequestId} for order {request.OrderId}");
        }

        public void RequestAnswerAutomaticallyAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats.\n\n" +
                $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                $"Datum och tid för uppdrag: {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToString("HH:mm")}" +
                GetPossibleInfoNotValidatedInterpreter(request);

            CreateEmail(GetRecipiantsFromOrder(request.Order),
                $"Förmedling har accepterat bokningsförfrågan {orderNumber}",
                body + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(request.Order.OrderId));

            NotifyBrokerOnAcceptedAnswer(request, orderNumber);
        }

        public void RequestAnswerApproved(Request request)
        {
            NotifyBrokerOnAcceptedAnswer(request, request.Order.OrderNumber);
        }

        public void RequestAnswerDenied(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerDenied, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Svar på bokningsförfrågan med boknings-ID {orderNumber} har underkänts",
                    $"Ert svar på bokningsförfrågan {orderNumber} underkändes med följande meddelande:\n{request.DenyMessage}. {GotoRequestPlain(request.RequestId)}",
                    $"Ert svar på bokningsförfrågan {orderNumber} underkändes med följande meddelande:<br />{request.DenyMessage}. {GotoRequestButton(request.RequestId)}",
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerDenied, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                     new RequestAnswerDeniedModel
                     {
                         OrderNumber = orderNumber,
                         Message = request.DenyMessage
                     },
                     webhook.ContactInformation,
                     NotificationType.RequestAnswerDenied,
                     webhook.RecipientUserId
                 );
            }
        }

        public void RequestExpired(Request request)
        {
            var orderNumber = request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToInactivity, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Bokningsförfrågan {orderNumber} har gått vidare till nästa förmedling i rangordningen",
                    $"Ni har inte bekräftat bokningsförfrågan {orderNumber} från {request.Order.CustomerOrganisation.Name}.\nTidsfristen enligt ramavtal har nu gått ut. {GotoRequestPlain(request.RequestId)}",
                    $"Ni har inte bekräftat bokningsförfrågan {orderNumber} från {request.Order.CustomerOrganisation.Name}.<br />Tidsfristen enligt ramavtal har nu gått ut. {GotoRequestButton(request.RequestId)}",
                    true);
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToInactivity, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestLostDueToInactivityModel
                    {
                        OrderNumber = orderNumber,
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestLostDueToInactivity,
                    webhook.RecipientUserId
                );
            }
        }

        public void ComplaintCreated(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintCreated, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"En reklamation har registrerats för tolkuppdrag med boknings-ID {orderNumber}",
                $@"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har skapats.\nReklamationstyp:\n
{complaint.ComplaintType.GetDescription()}\n\nAngiven reklamationsbeskrivning:\n
{complaint.ComplaintMessage} 
{GotoRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                $@"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har skapats.<br />Reklamationstyp:<br />
{complaint.ComplaintType.GetDescription()}<br /><br />Angiven reklamationsbeskrivning:<br />
{complaint.ComplaintMessage}
{GotoRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                true
            );
            }
            var webhook = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new ComplaintMessageModel
                    {
                        OrderNumber = orderNumber,
                        ComplaintType = complaint.ComplaintType.GetCustomName(),
                        Message = complaint.ComplaintMessage
                    },
                    webhook.ContactInformation,
                    NotificationType.ComplaintCreated,
                    webhook.RecipientUserId
                );

            }
        }

        public void ComplaintConfirmed(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.CreatedByUser.Email, $"Reklamation kopplad till tolkuppdrag {orderNumber} har godtagits",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits {GotoOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits {GotoOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintDisputed(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.CreatedByUser.Email, $"Reklamation kopplad till tolkuppdrag {orderNumber} har bestridits",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage} {GotoOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har bestridits med följande meddelande:<br />{complaint.AnswerMessage} {GotoOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintDisputePendingTrial(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputePendingTrial, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ert bestridande av reklamation avslogs för tolkuppdrag {orderNumber}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har avslagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {GotoRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har avslagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {GotoRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputePendingTrial, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new ComplaintMessageModel
                    {
                        OrderNumber = orderNumber,
                        ComplaintType = complaint.ComplaintType.GetCustomName(),
                        Message = complaint.AnswerDisputedMessage
                    },
                    webhook.ContactInformation,
                    NotificationType.ComplaintDisputePendingTrial,
                    webhook.RecipientUserId
                );
            }
        }

        public void ComplaintTerminatedAsDisputeAccepted(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputedAccepted, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ert bestridande av reklamation har godtagits för tolkuppdrag {orderNumber}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {GotoRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {GotoRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputedAccepted, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new ComplaintMessageModel
                    {
                        OrderNumber = orderNumber,
                        ComplaintType = complaint.ComplaintType.GetCustomName(),
                        Message = complaint.AnswerDisputedMessage
                    },
                    webhook.ContactInformation,
                    NotificationType.ComplaintDisputedAccepted,
                    webhook.RecipientUserId
                );
            }
        }

        public void RequisitionCreated(Requisition requisition)
        {
            var order = requisition.Request.Order;
            CreateEmail(GetRecipiantsFromOrder(order, true),
                $"En rekvisition har registrerats för tolkuppdrag {order.OrderNumber}",
                $"En rekvisition har registrerats för tolkuppdrag med boknings-ID {order.OrderNumber}. {GotoOrderPlain(order.OrderId, HtmlHelper.ViewTab.Requisition)}",
                $"En rekvisition har registrerats för tolkuppdrag med boknings-ID {order.OrderNumber}. {GotoOrderButton(order.OrderId, HtmlHelper.ViewTab.Requisition)}"
            );
        }

        public void RequisitionReviewed(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $@"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har granskats.

Sammanställning:

{GetRequisitionPriceInformationForMail(requisition)}";
            var email = GetBrokerNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionReviewed, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har granskats",
                    body + GotoRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    HtmlHelper.ToHtmlBreak(body) + GotoRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionReviewed, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(new RequisitionReviewedModel
                {
                    OrderNumber = orderNumber
                },
                webhook.ContactInformation,
                webhook.NotificationType,
                webhook.RecipientUserId);
            }
        }

        public void RequisitionCommented(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har kommenterats av myndighet. Följande kommentar har angivits:\n{requisition.CustomerComment}";
            var email = GetBrokerNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionCommented, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har kommenterats",
                    body + GotoRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    HtmlHelper.ToHtmlBreak(body) + GotoRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionCommented, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(new RequisitionCommentedModel
                {
                    OrderNumber = orderNumber,
                    Message = requisition.CustomerComment
                },
                webhook.ContactInformation,
                webhook.NotificationType,
                webhook.RecipientUserId);
            }
        }

        public void RequestAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats. {requireApprovementText}\n\n" +
                    $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                    $"Datum och tid för uppdrag: {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToString("HH:mm")}" +
                    GetPossibleInfoNotValidatedInterpreter(request);

            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat bokningsförfrågan {orderNumber}",
                body + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(request.Order.OrderId));
        }

        private string GetPossibleInfoNotValidatedInterpreter(Request request)
        {
            bool? isInterpreterVerified = request.InterpreterCompetenceVerificationResultOnAssign.HasValue ? (bool?)(request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.Validated) : null;
            return (_tolkBaseOptions.Tellus.IsActivated && isInterpreterVerified.HasValue && !isInterpreterVerified.Value) ? "\n\nObservera att tillsatt tolk för tolkuppdraget inte finns registrerad i Kammarkollegiets tolkregister med tillsatt kompetensnivå för detta språk. Risk finns att ställda krav på kompetensnivå inte uppfylls." : string.Empty;
        }

        public void RequestDeclinedByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på bokningsförfrågan {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till bokningsförfrågan med följande meddelande:\n{request.DenyMessage}";
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har tackat nej till bokningsförfrågan {orderNumber}",
                body + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(request.Order.OrderId));
        }

        public void RequestCancelledByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $"Förmedling {request.Ranking.Broker.Name} har avbokat tolkuppdraget med boknings-ID {orderNumber} med meddelande:\n{request.CancelMessage}";
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har avbokat tolkuppdraget med boknings-ID {orderNumber}",
                body + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(request.Order.OrderId));
        }

        public void RequestReplamentOrderAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    var body = $"Svar på ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Ersättningsuppdrag har accepterats. {requireApprovementText}";
                    CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        body + GotoOrderPlain(request.Order.OrderId),
                        HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(request.Order.OrderId));
                    break;
                case RequestStatus.Approved:
                    var bodyAppr = $"Ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har accepteras. Inga förändrade krav finns, tolkuppdrag är klart för utförande.";
                    CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        bodyAppr + GotoOrderPlain(request.Order.OrderId),
                        HtmlHelper.ToHtmlBreak(bodyAppr) + GotoOrderButton(request.Order.OrderId));
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestReplamentOrderAccepted)} cannot send notifications on requests with status: {request.Status.ToString()}");
            }
        }

        public void RequestReplamentOrderDeclinedByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;

            var body = $"Svar på ersättningsuppdrag {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} " +
                $"har tackat nej till ersättningsuppdrag med följande meddelande:\n{request.DenyMessage}";

            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har tackat nej till ersättningsuppdrag {orderNumber}",
                $"{body} {GotoOrderPlain(request.Order.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GotoOrderButton(request.Order.OrderId)}");
        }

        public void RequestChangedInterpreter(Request request)
        {
            string orderNumber = request.Order.OrderNumber;

            var body = $"Nytt svar på bokningsförfrågan med boknings-ID {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk för uppdraget.\n\n" +
                (request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved ?
                    requireApprovementText :
                    "Inga förändrade krav finns, bokningsförfrågan behåller sin nuvarande status.") +
                    GetPossibleInfoNotValidatedInterpreter(request);
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har bytt tolk för uppdrag med boknings-ID {orderNumber}",
                $"{body} {GotoOrderPlain(request.Order.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GotoOrderButton(request.Order.OrderId)}");
        }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User)
        {
            string orderNumber = request.Order.OrderNumber;
            //Broker
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestReplacedInterpreterAccepted, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Byte av tolk godkänt för boknings-ID {orderNumber}",
                $"Bytet av tolk har godkänts för tolkuppdrag med boknings-ID {orderNumber}. {GotoRequestPlain(request.RequestId)}",
                $"Bytet av tolk har godkänts för tolkuppdrag med boknings-ID {orderNumber}. {GotoRequestButton(request.RequestId)}",
                true
            );
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestReplacedInterpreterAccepted, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestChangedInterpreterAcceptedModel
                    {
                        OrderNumber = orderNumber,
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestReplacedInterpreterAccepted,
                    webhook.RecipientUserId
                );
            }
            //
            //Creator
            switch (changeOrigin)
            {
                case InterpereterChangeAcceptOrigin.NoNeedForUserAccept:
                    var bodyNoAccept = $"Nytt svar på bokningsförfrågan {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk för uppdraget.\n\n" +
                        "Inga förändrade krav finns, bokningsförfrågan behåller sin nuvarande status.";
                    CreateEmail(request.Order.CreatedByUser.Email, $"Förmedling har bytt tolk för uppdrag med boknings-ID {orderNumber}",
                        $"{bodyNoAccept} {GotoOrderPlain(request.Order.OrderId)}",
                        $"{HtmlHelper.ToHtmlBreak(bodyNoAccept)} {GotoOrderButton(request.Order.OrderId)}"
                    );
                    break;
                case InterpereterChangeAcceptOrigin.User:
                    //No mail to customer if it was the customer that accepted.
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestChangedInterpreterAccepted)} failed to send mail to customer. {changeOrigin.ToString()} is not a handled {nameof(InterpereterChangeAcceptOrigin)}");
            }
        }

        public void RemindUnhandledRequest(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            string body =  $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} väntar på hantering. Bokningsförfrågan har "
            + (request.Status == RequestStatus.AcceptedNewInterpreterAppointed ? "ändrats med ny tolk. " : "accepterats. ")
            + requireApprovementText;

            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Bokningsförfrågan {orderNumber} väntar på hantering",
                body + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(request.Order.OrderId));
        }

        public void CreateEmail(string recipient, string subject, string plainBody, bool isBrokerMail = false, bool addContractInfo = true)
        {
            CreateEmail(new[] { recipient }, subject, plainBody, HtmlHelper.ToHtmlBreak(plainBody), isBrokerMail, addContractInfo);
        }

        public void CreateEmail(string recipient, string subject, string plainBody, string htmlBody, bool isBrokerMail = false, bool addContractInfo = true)
        {
            CreateEmail(new[] { recipient }, subject, plainBody, htmlBody, isBrokerMail, addContractInfo);
        }

        private void CreateEmail(IEnumerable<string> recipients, string subject, string plainBody, string htmlBody, bool isBrokerMail = false, bool addContractInfo = true)
        {
            string subjectPrepend = string.Empty;
            if (!string.IsNullOrEmpty(TolkBaseOptions.Env.Name) && _tolkBaseOptions.Env.Name.ToLower() != "production")
            {
                subjectPrepend = $"({_tolkBaseOptions.Env.Name}) ";
            }

            string noReply = "Detta e-postmeddelande går inte att svara på.";
            string handledBy = "Detta ärende hanteras i Kammarkollegiets Tolktjänst.";
            string contractInfo = "Avrop från ramavtal för tolkförmedlingstjänster 23.3-9066-16";

            foreach (string recipient in recipients)
            {
                _dbContext.Add(new OutboundEmail(
                    recipient,
                    subjectPrepend + subject,
                    $"{plainBody}\n\n{noReply}" + (isBrokerMail ? $"\n\n{handledBy}" : "") + (addContractInfo ? $"\n\n{contractInfo}": ""),
                    $"{htmlBody}<br/><br/>{noReply}" + (isBrokerMail ? $"<br/><br/>{handledBy}" : "") + (addContractInfo ? $"<br/><br/>{contractInfo}" : ""),
                    _clock.SwedenNow));
            }
            _dbContext.SaveChanges();
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
                invoiceInfo += $"Följande tolktaxa har använts för beräkning: {priceInfo.PriceListTypeDescription} {priceInfo.CompetencePriceDescription}\n\n";
                foreach (DisplayPriceRow dpr in priceInfo.DisplayPriceRows)
                {
                    invoiceInfo += $"{dpr.Description}:\n{dpr.Price.ToString("#,0.00 SEK")}\n\n";
                }
                invoiceInfo += $"Total summa: {priceInfo.TotalPrice.ToString("#,0.00 SEK")}";
                return invoiceInfo;
            }
        }

        private void NotifyBrokerOnAcceptedAnswer(Request request, string orderNumber)
        {
            //Broker part
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerApproved, NotificationChannel.Email);
            if (email != null)
            {
                var body = $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av tolk på bokningsförfrågan {orderNumber}.";
                CreateEmail(email.ContactInformation, $"Tolkuppdrag med boknings-ID {orderNumber} verifierat",
                        body + GotoOrderPlain(request.Order.OrderId),
                        body + GotoOrderButton(request.Order.OrderId),
                        true);
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerApproved, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestAnswerApprovedModel
                    {
                        OrderNumber = orderNumber
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestAnswerApproved,
                    webhook.RecipientUserId
                );
            }
        }

        private static IEnumerable<string> GetRecipiantsFromOrder(Order order, bool sendToContactPerson = false)
        {
            yield return order.CreatedByUser.Email;
            if (sendToContactPerson && order.ContactPersonId.HasValue)
            {
                yield return order.ContactPersonUser.Email;
            }
        }

        private BrokerNotificationSettings GetBrokerNotificationSettings(int brokerId, NotificationType type, NotificationChannel channel)
        {
            if (!BrokerNotificationSettings.Any(b => b.BrokerId == brokerId) && channel == NotificationChannel.Email)
            {
                return new BrokerNotificationSettings
                {
                    ContactInformation = _dbContext.Brokers.Single(b => b.BrokerId == brokerId).EmailAddress,
                };
            }

            return BrokerNotificationSettings.SingleOrDefault(b => b.BrokerId == brokerId && b.NotificationType == type && b.NotificationChannel == channel);
        }

        private IEnumerable<BrokerNotificationSettings> BrokerNotificationSettings
        {
            get
            {
                if (!_cache.TryGetValue(brokerSettingsCacheKey, out IEnumerable<BrokerNotificationSettings> brokerNotificationSettings))
                {
                    brokerNotificationSettings = _dbContext.Users.Include(u => u.NotificationSettings)
                        .Where(u => u.BrokerId != null && u.IsApiUser)
                        .SelectMany(u => u.NotificationSettings)
                        .Select(n => new BrokerNotificationSettings
                        {
                            BrokerId = n.User.BrokerId.Value,
                            ContactInformation = n.ConnectionInformation ?? (n.NotificationChannel == NotificationChannel.Email ? n.User.Email : null),
                            NotificationChannel = n.NotificationChannel,
                            NotificationType = n.NotificationType,
                            RecipientUserId = n.UserId
                        }).ToList().AsReadOnly();
                    _cache.Set(brokerSettingsCacheKey, brokerNotificationSettings, DateTimeOffset.Now.AddDays(1));
                }
                return brokerNotificationSettings;
            }
        }

        public ITolkBaseOptions TolkBaseOptions => _tolkBaseOptions;

        //SHOULD PROBABLY NOT BE HERE AT ALL...
        public void FlushNotificationSettings()
        {
            _cache.Remove(brokerSettingsCacheKey);
        }

        private void CreateWebHookCall(WebHookPayloadBaseModel payload, string recipientUrl, NotificationType type, int userId)
        {
            _dbContext.Add(new OutboundWebHookCall(
                recipientUrl,
                JsonConvert.SerializeObject(payload, Formatting.Indented),
                type,
                _clock.SwedenNow,
                userId));
            _dbContext.SaveChanges();
        }

        private string GotoOrderPlain(int orderId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default)
        {
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return $"\n\n\nGå till bokning: {HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}";
                case HtmlHelper.ViewTab.Requisition:
                    return $"\n\n\nGå till rekvisition: {HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}?tab=requisition";
                case HtmlHelper.ViewTab.Complaint:
                    return $"\n\n\nGå till reklamation: {HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}?tab=complaint";
            }
        }

        private string GotoRequestPlain(int requestId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default)
        {
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return $"\n\n\nGå till bokningsförfrågan: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}";
                case HtmlHelper.ViewTab.Requisition:
                    return $"\n\n\nGå till rekvisition: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}?tab=requisition";
                case HtmlHelper.ViewTab.Complaint:
                    return $"\n\n\nGå till reklamation: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}?tab=complaint";
            }
        }

        private string GotoOrderButton(int orderId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, string textOverride = null, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            if (!string.IsNullOrEmpty(textOverride))
            {
                return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId), textOverride);
            }
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId), "Till bokning");
                case HtmlHelper.ViewTab.Requisition:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}?tab=requisition", "Till rekvisition");
                case HtmlHelper.ViewTab.Complaint:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}?tab=complaint", "Till reklamation");
            }
        }

        private string GotoRequestButton(int requestId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, string textOverride = null, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            if (!string.IsNullOrEmpty(textOverride))
            {
                return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId), textOverride);
            }
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId), "Till bokning");
                case HtmlHelper.ViewTab.Requisition:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}?tab=requisition", "Till rekvisition");
                case HtmlHelper.ViewTab.Complaint:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}?tab=complaint", "Till reklamation");
            }
        }

        private static RequestModel GetRequestModel(Request request)
        {
            var order = request.Order;

            return new RequestModel
            {
                CreatedAt = request.CreatedAt,
                OrderNumber = order.OrderNumber,
                Customer = order.CustomerOrganisation.Name,
                //D2 pads any single digit with a zero 1 -> "01"
                Region = order.Region.RegionId.ToString("D2"),
                Language = new LanguageModel
                {
                    Key = request.Order.Language?.ISO_639_Code,
                    Description = order.OtherLanguage ?? order.Language.Name,
                },
                ExpiresAt = request.ExpiresAt,
                StartAt = order.StartAt,
                EndAt = order.EndAt,
                Locations = order.InterpreterLocations.Select(l => new LocationModel
                {
                    ContactInformation = l.OffSiteContactInformation ?? l.FullAddress,
                    Rank = l.Rank,
                    Key = EnumHelper.GetCustomName(l.InterpreterLocation)
                }),
                CompetenceLevels = order.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = EnumHelper.GetCustomName(c.CompetenceLevel),
                    Rank = c.Rank ?? 0
                }),
                AllowExceedingTravelCost = order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                AssignentType = EnumHelper.GetCustomName(order.AssignentType),
                Description = order.Description,
                CompetenceLevelsAreRequired = order.SpecificCompetenceLevelRequired,
                Requirements = order.Requirements.Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderRequirementId,
                    RequirementType = EnumHelper.GetCustomName(r.RequirementType)
                }),
                Attachments = order.Attachments.Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.Attachment.FileName
                }),
                //Need to aggregate the price list types
                PriceInformation = new PriceInformationModel
                {
                    PriceCalculatedFromCompetenceLevel = order.PriceCalculatedFromCompetenceLevel.GetCustomName(),
                    PriceRows = order.PriceRows.GroupBy(r => r.PriceRowType)
                        .Select(p => new PriceRowModel
                        {
                            Description = p.Key.GetDescription(),
                            PriceRowType = p.Key.GetCustomName(),
                            Price = p.Count() == 1 ? p.Sum(s => s.TotalPrice) : 0,
                            CalculationBase = p.Count() == 1 ? p.Single()?.PriceCalculationCharge?.ChargePercentage : null,
                            CalculatedFrom = EnumHelper.Parent<PriceRowType, PriceRowType?>(p.Key)?.GetCustomName(),
                            PriceListRows = p.Where(l => l.PriceListRowId != null).Select(l => new PriceRowListModel
                            {
                                PriceListRowType = l.PriceListRow.PriceListRowType.GetCustomName(),
                                Description = l.PriceListRow.PriceListRowType.GetDescription(),
                                Price = l.Price,
                                Quantity = l.Quantity
                            })
                        })
                }
            };
        }

        private Request GetRequest(int id)
        {
            return _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(o => o.Attachment)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Single(o => o.RequestId == id);
        }

        public bool ResendWebHook(OutboundWebHookCall failedCall)
        {
            int? brokerId = _dbContext.Users.SingleOrDefault(u => u.Id == failedCall.RecipientUserId)?.BrokerId;
            if (brokerId == null)
            {
                return false;
            }

            var webhook = GetBrokerNotificationSettings((int)brokerId, failedCall.NotificationType, NotificationChannel.Webhook);

            if (webhook == null)
            {
                return false;
            }

            OutboundWebHookCall newCall = new OutboundWebHookCall(
                webhook.ContactInformation,
                failedCall.Payload,
                failedCall.NotificationType,
                _clock.SwedenNow,
                webhook.RecipientUserId);

            _dbContext.OutboundWebHookCalls.Add(newCall);
            _dbContext.SaveChanges();
            failedCall.ResentHookId = newCall.OutboundWebHookCallId;
            _dbContext.SaveChanges();

            return true;
        }
    }
}
