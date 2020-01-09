using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class NotificationService : INotificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISwedishClock _clock;
        private readonly IMemoryCache _cache;
        private readonly ITolkBaseOptions _tolkBaseOptions;
        private readonly string _senderPrepend;

        private const string brokerSettingsCacheKey = nameof(brokerSettingsCacheKey);

        private const string requireApprovementText = "Observera att ni måste godkänna tillsatt tolk för tolkuppdraget innan bokning kan slutföras eftersom ni har begärt att få förhandsgodkänna resekostnader. Om godkännande inte görs kommer bokningen att annulleras.";

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock,
            IMemoryCache cache,
            ITolkBaseOptions tolkBaseOptions
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _cache = cache;
            _tolkBaseOptions = tolkBaseOptions;
            _senderPrepend = !string.IsNullOrWhiteSpace(_tolkBaseOptions?.Env.DisplayName) ? $"{_tolkBaseOptions?.Env.DisplayName} " : string.Empty;
        }

        public void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(OrderCancelledByCustomer), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;

            //customer send email with info about requisition created 
            if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
            {
                string body = $"Rekvisition har skapats pga att myndigheten har avbokat uppdrag med boknings-ID {orderNumber}. Uppdraget avbokades med detta meddelande:\n{request.CancelMessage}\n" +
                     (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, i de fall något ersättningsuppdrag inte kan ordnas av kund. Observera att ersättning kan tillkomma för eventuell tidsspillan som tolken skulle behövt ta ut för genomförande av aktuellt uppdrag. Även kostnader avseende resor och boende som ej är avbokningsbara, alternativt avbokningskostnader för resor och boende som avbokats kan tillkomma. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna."
                     : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.");
                CreateEmail(GetRecipientsFromOrder(request.Order, true), $"Rekvisition har skapats pga avbokat uppdrag boknings-ID {orderNumber}",
                    body + GoToOrderPlain(request.Order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                    true);
            }
            //broker
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCancelledByCustomer, NotificationChannel.Email);
            if (email != null)
            {
                if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
                {
                    string body = $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                         $"Uppdraget har boknings-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}." +
                         (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, i de fall något ersättningsuppdrag inte kan ordnas av kund. Observera att ersättning kan tillkomma för eventuell tidsspillan som tolken skulle behövt ta ut för genomförande av aktuellt uppdrag. Även kostnader avseende resor och boende som ej är avbokningsbara, alternativt avbokningskostnader för resor och boende som avbokats kan tillkomma. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.");
                    CreateEmail(email.ContactInformation, $"Avbokat tolkuppdrag boknings-ID {orderNumber}",
                        body + GoToRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(request.RequestId),
                        true);
                }
                else
                {
                    var body = $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                        $"Uppdraget har boknings-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}.";
                    CreateEmail(email.ContactInformation, $"Avbokad förfrågan boknings-ID {orderNumber}",
                        body + GoToRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(request.RequestId),
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

        public void OrderContactPersonChanged(Order order, AspNetUser previousContactUser)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(OrderContactPersonChanged), nameof(NotificationService));
            AspNetUser currentContactUser = order.ContactPersonUser;

            string orderNumber = order.OrderNumber;

            string subject = $"Behörighet ändrad för tolkuppdrag boknings-ID {orderNumber}";

            if (!string.IsNullOrEmpty(previousContactUser?.Email))
            {
                string body = $"Behörighet att granska rekvisition har ändrats. Du har inte längre denna behörighet för bokning {orderNumber}.";
                CreateEmail(previousContactUser.Email, subject,
                    body + GoToOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(order.OrderId));
            }
            if (!string.IsNullOrEmpty(currentContactUser?.Email))
            {
                string body = $"Behörighet att granska rekvisition har ändrats. Du har nu behörighet att utföra detta för bokning {orderNumber}.";
                CreateEmail(currentContactUser.Email, subject,
                    body + GoToOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(order.OrderId));
            }
        }

        public void OrderReplacementCreated(Order order)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(OrderReplacementCreated), nameof(NotificationService));
            Request oldRequest = order.Requests.SingleOrDefault(r => r.Status == RequestStatus.CancelledByCreator);

            Order replacementOrder = order.ReplacedByOrder;
            Request replacementRequest = replacementOrder.Requests.Single();
            var email = GetBrokerNotificationSettings(replacementRequest.Ranking.BrokerId, NotificationType.RequestReplacementCreated, NotificationChannel.Email);
            if (email != null)
            {
                var bodyPlain = $"\tOrginal Start: {order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tOrginal Slut: {order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tErsättning Start: {replacementOrder.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tErsättning Slut: {replacementOrder.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tSvara senast: {replacementRequest.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}\n\n\n" +
                   $"Gå till ersättningsuppdrag: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, replacementRequest.RequestId)}\n" +
                   $"Gå till ursprungligt uppdrag: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, oldRequest.RequestId)}";
                var bodyHtml = $@"
<ul>
<li>Orginal Start: {order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Orginal Slut: {order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Start: {replacementOrder.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Slut: {replacementOrder.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Svara senast: {replacementRequest.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{GoToRequestButton(replacementRequest.RequestId, textOverride: "Gå till ersättningsuppdrag", autoBreakLines: false)}</div>
<div>{GoToRequestButton(oldRequest.RequestId, textOverride: "Gå till ursprungligt uppdrag", autoBreakLines: false)}</div>";
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
                   NotificationType.RequestReplacementCreated,
                   webhook.RecipientUserId
               );
            }
        }

        public void OrderNoBrokerAccepted(Order order)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(OrderNoBrokerAccepted), nameof(NotificationService));
            CreateEmail(GetRecipientsFromOrder(order),
                $"Bokningsförfrågan {order.OrderNumber} fick ingen tolk",
                $"Ingen förmedling kunde tillsätta en tolk för bokningsförfrågan {order.OrderNumber}. {GoToOrderPlain(order.OrderId)}",
                $"Ingen förmedling kunde tillsätta en tolk för bokningsförfrågan {order.OrderNumber}. {GoToOrderButton(order.OrderId)}"
            );
        }

        public void OrderGroupNoBrokerAccepted(OrderGroup terminatedOrderGroup)
        {
            NullCheckHelper.ArgumentCheckNull(terminatedOrderGroup, nameof(OrderGroupNoBrokerAccepted), nameof(NotificationService));
            CreateEmail(GetRecipientsFromOrderGroup(terminatedOrderGroup),
                $"Sammanhållen bokningsförfrågan {terminatedOrderGroup.OrderGroupNumber} fick ingen tolk",
                $"Ingen förmedling kunde tillsätta tolk för den sammanhållna bokningsförfrågan {terminatedOrderGroup.OrderGroupNumber}. {GoToOrderGroupPlain(terminatedOrderGroup.OrderGroupId)}",
                $"Ingen förmedling kunde tillsätta tolk för den sammanhållna bokningsförfrågan {terminatedOrderGroup.OrderGroupNumber}. {GoToOrderGroupButton(terminatedOrderGroup.OrderGroupId)}"
            );
        }

        public void RequestCreated(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCreated), nameof(NotificationService));
            var order = request.Order;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Email);
            if (email != null)
            {
                string bodyPlain = $"Bokningsförfrågan för tolkuppdrag {order.OrderNumber} från {order.CustomerOrganisation.Name} organisationsnummer {order.CustomerOrganisation.OrganisationNumber} har inkommit via {Constants.SystemName}. Observera att bekräftelse måste lämnas via avropstjänsten.\n" +
                    $"\tUppdragstyp: {EnumHelper.GetDescription(order.AssignmentType)}\n" +
                    $"\tRegion: {order.Region.Name}\n" +
                    $"\tSpråk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tStart: {order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSlut: {order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSvara senast: {request.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}\n\n\n" +
                    GoToRequestPlain(request.RequestId);
                string bodyHtml = $@"Bokningsförfrågan för tolkuppdrag {order.OrderNumber} från {order.CustomerOrganisation.Name} organisationsnummer {order.CustomerOrganisation.OrganisationNumber} har inkommit via {Constants.SystemName}. Observera att bekräftelse måste lämnas via avropstjänsten.<br />
<ul>
<li>Uppdragstyp: {EnumHelper.GetDescription(order.AssignmentType)}</li>
<li>Region: {order.Region.Name}</li>
<li>Språk: {order.OtherLanguage ?? order.Language?.Name}</li>
<li>Start: {order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Slut: {order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Svara senast: {request.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{GoToRequestButton(request.RequestId, textOverride: "Till förfrågan", autoBreakLines: false)}</div>";
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

        public void RequestGroupCreated(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupCreated), nameof(NotificationService));
            var orderGroup = requestGroup.OrderGroup;
            var email = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupCreated, NotificationChannel.Email);
            if (email != null)
            {
                string bodyPlain = $"Bokningsförfrågan för ett sammanhållet tolkuppdrag {orderGroup.OrderGroupNumber} från {orderGroup.CustomerOrganisation.Name} organisationsnummer {orderGroup.CustomerOrganisation.OrganisationNumber} har inkommit via {Constants.SystemName}. Observera att bekräftelse måste lämnas via avropstjänsten.\n" +
                    $"\tUppdragstyp: {EnumHelper.GetDescription(orderGroup.AssignmentType)}\n" +
                    $"\tRegion: {orderGroup.Region.Name}\n" +
                    $"\tSpråk: {orderGroup.LanguageName}\n" +
                    $"\tTillfällen: \n" +
                    $"{GetOccurences(orderGroup.Orders)}\n" +
                    $"\tSvara senast: {requestGroup.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}\n\n\n" +
                    GoToRequestGroupPlain(requestGroup.RequestGroupId);
                string bodyHtml = $@"Bokningsförfrågan för ett sammanhållet tolkuppdrag {orderGroup.OrderGroupNumber} från {orderGroup.CustomerOrganisation.Name} organisationsnummer {orderGroup.CustomerOrganisation.OrganisationNumber} har inkommit via {Constants.SystemName}. Observera att bekräftelse måste lämnas via avropstjänsten.<br />
                    <ul>
                    <li>Uppdragstyp: {EnumHelper.GetDescription(orderGroup.AssignmentType)}</li>
                    <li>Region: {orderGroup.Region.Name}</li>
                    <li>Språk: {orderGroup.LanguageName}</li>
                    <li>{GetOccurencesAsHtmlList(orderGroup.Orders)}</li>
                    <li>Svara senast: {requestGroup.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
                    </ul>
                    <div>{GoToRequestGroupButton(requestGroup.RequestGroupId, textOverride: "Till förfrågan", autoBreakLines: false)}</div>";
                CreateEmail(
                    email.ContactInformation,
                    $"Ny sammanhållen bokningsförfrågan registrerad: {orderGroup.OrderGroupNumber}",
                    bodyPlain,
                    bodyHtml,
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    GetRequestGroupModel(requestGroup),
                    webhook.ContactInformation,
                    NotificationType.RequestGroupCreated,
                    webhook.RecipientUserId
                );
            }
        }

        private string GetOccurences(IEnumerable<Order> orders)
        {
            var texts = orders.Select(o => $"\t\t{o.OrderNumber}: {o.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{o.EndAt.ToSwedishString("HH:mm")}").ToArray();
            return string.Join("\n", texts);
        }

        private string GetOccurencesAsHtmlList(IEnumerable<Order> orders)
        {
            var texts = orders.Select(o => $"<li>{o.OrderNumber}: {o.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{o.EndAt.ToSwedishString("HH:mm")}").ToArray();
            return $"<ul>{string.Join("\n", texts)}</ul>";
        }

        public void RequestCreatedWithoutExpiry(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCreatedWithoutExpiry), nameof(NotificationService));
            string body = $@"Bokningsförfrågan {request.Order.OrderNumber} måste kompletteras med sista svarstid innan den kan skickas till nästa förmedling för tillsättning.

Notera att er förfrågan INTE skickas vidare till nästa förmedling, tills dess sista svarstid är satt.";

            CreateEmail(GetRecipientsFromOrder(request.Order),
                $"Sista svarstid ej satt på bokningsförfrågan {request.Order.OrderNumber}",
                $"{body} {GoToOrderPlain(request.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderButton(request.OrderId)}");

            _logger.LogInformation($"Email created for customer regarding missing expiry on request {request.RequestId} for order {request.OrderId}");
        }

        public void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(newRequestGroup, nameof(RequestGroupCreatedWithoutExpiry), nameof(NotificationService));
            string orderGroupNumber = newRequestGroup.OrderGroup.OrderGroupNumber;
            string body = $@"Sammanhållen bokningsförfrågan {orderGroupNumber} måste kompletteras med sista svarstid innan den kan skickas till nästa förmedling för tillsättning.

Notera att er förfrågan INTE skickas vidare till nästa förmedling, tills dess sista svarstid är satt.";

            CreateEmail(GetRecipientsFromOrderGroup(newRequestGroup.OrderGroup),
                $"Sista svarstid ej satt på sammanhållen bokningsförfrågan {orderGroupNumber}",
                $"{body} {GoToOrderGroupPlain(newRequestGroup.OrderGroup.OrderGroupId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderGroupButton(newRequestGroup.OrderGroup.OrderGroupId)}");

            _logger.LogInformation($"Email created for customer regarding missing expiry on request group {newRequestGroup.RequestGroupId} for order group {newRequestGroup.OrderGroup.OrderGroupId}");
        }

        public void RequestAnswerAutomaticallyApproved(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAnswerAutomaticallyApproved), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats.\n\n" +
                OrderReferenceNumberInfo(request) +
                $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                InterpreterCompetenceInfo(request.CompetenceLevel) +
                GetPossibleInfoNotValidatedInterpreter(request);

            CreateEmail(GetRecipientsFromOrder(request.Order),
                $"Förmedling har accepterat bokningsförfrågan {orderNumber}",
                body + GoToOrderPlain(request.Order.OrderId, HtmlHelper.ViewTab.Default, true),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId, HtmlHelper.ViewTab.Default, null, true, true));

            NotifyBrokerOnAcceptedAnswer(request, orderNumber);
        }

        public void RequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupAnswerAutomaticallyApproved), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            Order order = requestGroup.OrderGroup.FirstOrder;

            var body = $"Svar på  sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats.\n\n" +
                $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                $"\tTillfällen: \n" +
                $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
            if (requestGroup.HasExtraInterpreter)
            {
                body += $"\nExtra tolk: \n" + GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForExtraInterpreter);
            }
            CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                $"Förmedling har accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId));
            NotifyBrokerOnAcceptedAnswer(requestGroup, orderGroupNumber);
        }

        public void RequestAnswerApproved(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAnswerApproved), nameof(NotificationService));
            NotifyBrokerOnAcceptedAnswer(request, request.Order.OrderNumber);
        }

        public void RequestAnswerDenied(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAnswerDenied), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerDenied, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Svar på bokningsförfrågan med boknings-ID {orderNumber} har underkänts",
                    $"Ert svar på bokningsförfrågan {orderNumber} underkändes med följande meddelande:\n{request.DenyMessage}. {GoToRequestPlain(request.RequestId)}",
                    $"Ert svar på bokningsförfrågan {orderNumber} underkändes med följande meddelande:<br />{request.DenyMessage}. {GoToRequestButton(request.RequestId)}",
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

        public void RequestGroupAnswerDenied(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupAnswerDenied), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            var email = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerDenied, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Svar på sammanhållen bokningsförfrågan med boknings-ID {orderGroupNumber} har underkänts",
                    $"Ert svar på sammanhållen bokningsförfrågan {orderGroupNumber} underkändes med följande meddelande:\n{requestGroup.DenyMessage}. {GoToRequestGroupPlain(requestGroup.RequestGroupId)}",
                    $"Ert svar på sammanhållen bokningsförfrågan {orderGroupNumber} underkändes med följande meddelande:<br />{requestGroup.DenyMessage}. {GoToRequestGroupButton(requestGroup.RequestGroupId)}",
                    true
                );
            }
            var webhook = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerDenied, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                     new RequestGroupAnswerDeniedModel
                     {
                         OrderGroupNumber = orderGroupNumber,
                         Message = requestGroup.DenyMessage
                     },
                     webhook.ContactInformation,
                     NotificationType.RequestGroupAnswerDenied,
                     webhook.RecipientUserId
                 );
            }
        }

        public void RequestExpired(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestExpired), nameof(NotificationService));
            var orderNumber = request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToInactivity, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Bokningsförfrågan {orderNumber} har gått vidare till nästa förmedling i rangordningen",
                    $"Ni har inte bekräftat bokningsförfrågan {orderNumber} från {request.Order.CustomerOrganisation.Name} organisationsnummer {request.Order.CustomerOrganisation.OrganisationNumber}.\nTidsfristen enligt ramavtal har nu gått ut. {GoToRequestPlain(request.RequestId)}",
                    $"Ni har inte bekräftat bokningsförfrågan {orderNumber} från {request.Order.CustomerOrganisation.Name} organisationsnummer {request.Order.CustomerOrganisation.OrganisationNumber}.<br />Tidsfristen enligt ramavtal har nu gått ut. {GoToRequestButton(request.RequestId)}",
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

        public void RequestGroupExpired(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupExpired), nameof(NotificationService));
            var orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            var email = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupLostDueToInactivity, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Sammanhållen bokningsförfrågan {orderGroupNumber} har gått vidare till nästa förmedling i rangordningen",
                    $"Ni har inte bekräftat den sammanhållna bokningsförfrågan {orderGroupNumber} från {requestGroup.OrderGroup.CustomerOrganisation.Name} organisationsnummer {requestGroup.OrderGroup.CustomerOrganisation.OrganisationNumber}.\nTidsfristen enligt ramavtal har nu gått ut. {GoToRequestGroupPlain(requestGroup.RequestGroupId)}",
                    $"Ni har inte bekräftat den sammanhållna bokningsförfrågan {orderGroupNumber} från {requestGroup.OrderGroup.CustomerOrganisation.Name} organisationsnummer {requestGroup.OrderGroup.CustomerOrganisation.OrganisationNumber}.<br />Tidsfristen enligt ramavtal har nu gått ut. {GoToRequestGroupButton(requestGroup.RequestGroupId)}",
                    true);
            }
            var webhook = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupLostDueToInactivity, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestGroupLostDueToInactivityModel
                    {
                        OrderGroupNumber = orderGroupNumber,
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestGroupLostDueToInactivity,
                    webhook.RecipientUserId
                );
            }
        }

        public void ComplaintCreated(Complaint complaint)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintCreated), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintCreated, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"En reklamation har registrerats för tolkuppdrag med boknings-ID {orderNumber}",
                $@"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har skapats.
Reklamationstyp:
{complaint.ComplaintType.GetDescription()}

Angiven reklamationsbeskrivning:
{complaint.ComplaintMessage} 
{GoToRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                $@"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har skapats.<br />Reklamationstyp:<br />
{complaint.ComplaintType.GetDescription()}<br /><br />Angiven reklamationsbeskrivning:<br />
{complaint.ComplaintMessage}
{GoToRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
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
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintConfirmed), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.ContactEmail, $"Reklamation kopplad till tolkuppdrag {orderNumber} har godtagits",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits {GoToOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits {GoToOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintDisputed(Complaint complaint)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintDisputed), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.ContactEmail, $"Reklamation kopplad till tolkuppdrag {orderNumber} har bestridits",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage} {GoToOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har bestridits med följande meddelande:<br />{complaint.AnswerMessage} {GoToOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintDisputePendingTrial(Complaint complaint)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintDisputePendingTrial), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputePendingTrial, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ert bestridande av reklamation avslogs för tolkuppdrag {orderNumber}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har avslagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {GoToRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har avslagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {GoToRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
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
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintTerminatedAsDisputeAccepted), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetBrokerNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputedAccepted, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ert bestridande av reklamation har godtagits för tolkuppdrag {orderNumber}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {GoToRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {GoToRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
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
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(RequisitionCreated), nameof(NotificationService));
            var order = requisition.Request.Order;
            CreateEmail(GetRecipientsFromOrder(order, true),
                $"En rekvisition har registrerats för tolkuppdrag {order.OrderNumber}",
                $"En rekvisition har registrerats för tolkuppdrag med boknings-ID {order.OrderNumber}. {GoToOrderPlain(order.OrderId, HtmlHelper.ViewTab.Requisition)}",
                $"En rekvisition har registrerats för tolkuppdrag med boknings-ID {order.OrderNumber}. {GoToOrderButton(order.OrderId, HtmlHelper.ViewTab.Requisition)}"
            );
        }

        public void RequisitionReviewed(Requisition requisition)
        {
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(RequisitionReviewed), nameof(NotificationService));
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $@"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har granskats.

Sammanställning:

{GetRequisitionPriceInformationForMail(requisition)}";
            var email = GetBrokerNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionReviewed, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har granskats",
                    body + GoToRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
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
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(RequisitionCommented), nameof(NotificationService));
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har kommenterats av myndighet. Följande kommentar har angivits:\n{requisition.CustomerComment}";
            var email = GetBrokerNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionCommented, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har kommenterats",
                    body + GoToRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
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

        public void CustomerCreated(CustomerOrganisation customer)
        {
            NullCheckHelper.ArgumentCheckNull(customer, nameof(CustomerCreated), nameof(NotificationService));
            var body = $"Myndigheten {customer.Name} med organisationsnummer {customer.OrganisationNumber} har lagts till i systemet. \n Vid användning av tjänstens API kan myndigheten identifieras med denna identifierare: {customer.OrganisationPrefix}";
            foreach (int brokerId in _dbContext.Brokers.Select(b => b.BrokerId).ToList())
            {
                var email = GetBrokerNotificationSettings(brokerId, NotificationType.CustomerAdded, NotificationChannel.Email);
                if (email != null)
                {
                    CreateEmail(email.ContactInformation, $"En ny myndighet har lagts upp i systemet.", body);
                }
                var webhook = GetBrokerNotificationSettings(brokerId, NotificationType.CustomerAdded, NotificationChannel.Webhook);
                if (webhook != null)
                {
                    CreateWebHookCall(new CustomerCreatedModel
                    {
                        Key = customer.OrganisationPrefix,
                        OrganisationNumber = customer.OrganisationNumber,
                        PriceListType = customer.PriceListType.GetCustomName(),
                        Name = customer.Name,
                        Description = customer.ParentCustomerOrganisationId != null ? $"Organiserad under {customer.ParentCustomerOrganisation.Name}" : null,
                        TravelCostAgreementType = customer.TravelCostAgreementType.GetCustomName()
                    },
                    webhook.ContactInformation,
                    webhook.NotificationType,
                    webhook.RecipientUserId);
                }
            }
        }

        public void RequestAccepted(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAccepted), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats. {requireApprovementText}\n\n" +
                    OrderReferenceNumberInfo(request) +
                    $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                    $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                    InterpreterCompetenceInfo(request.CompetenceLevel) +
                    GetPossibleInfoNotValidatedInterpreter(request);

            CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har accepterat bokningsförfrågan {orderNumber}",
                body + GoToOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId));
        }

        private string OrderReferenceNumberInfo(Request request)
        {
            return string.IsNullOrWhiteSpace(request.Order.CustomerReferenceNumber) ? string.Empty : $"Myndighetens ärendenummer: {request.Order.CustomerReferenceNumber}\n";
        }

        private string InterpreterCompetenceInfo(int? competenceInfo)
        {
            return $"Tolkens kompetensnivå: {((CompetenceAndSpecialistLevel?)competenceInfo)?.GetDescription() ?? "Information saknas"}";
        }

        public void RequestGroupAccepted(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupAccepted), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            Order order = requestGroup.OrderGroup.FirstOrder;
            var body = $"Svar på sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats. {requireApprovementText}\n\n" +
                $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                $"\tTillfällen: \n" +
                $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
            if (requestGroup.HasExtraInterpreter)
            {
                body += $"\nExtra tolk: \n" + GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForExtraInterpreter);
            }
            CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                $"Förmedling har accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId));
        }

        public void RequestGroupAnswerApproved(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupAnswerApproved), nameof(NotificationService));
            NotifyBrokerOnAcceptedAnswer(requestGroup, requestGroup.OrderGroup.OrderGroupNumber);
        }

        public void RequestDeclinedByBroker(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestDeclinedByBroker), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på bokningsförfrågan {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till bokningsförfrågan med följande meddelande:\n{request.DenyMessage} \n\nBokningsförfrågan skickas nu automatiskt vidare till nästa förmedling enligt rangordningen förutsatt att det finns ytterligare förmedlingar att fråga. I de fall en bokningsförfrågan avslutas på grund av att ingen förmedling har kunnat tillsätta en tolk så skickas ett e-postmeddelande till er om detta.";
            CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har tackat nej till bokningsförfrågan {orderNumber}",
                body + GoToOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId));
        }

        public void RequestGroupDeclinedByBroker(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupDeclinedByBroker), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            var body = $"Svar på sammanhållna bokningsförfrågan {orderGroupNumber} har inkommit. Förmedling {requestGroup.Ranking.Broker.Name} har tackat nej till den sammanhållna bokningsförfrågan med följande meddelande:\n{requestGroup.DenyMessage} \n\nBokningsförfrågan skickas nu automatiskt vidare till nästa förmedling enligt rangordningen förutsatt att det finns ytterligare förmedlingar att fråga. I de fall en bokningsförfrågan avslutas på grund av att ingen förmedling har kunnat tillsätta en tolk så skickas ett e-postmeddelande till er om detta.";
            CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup), $"Förmedling har tackat nej till den sammanhållna bokningsförfrågan {orderGroupNumber}",
                body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId));
        }

        public void RequestCancelledByBroker(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCancelledByBroker), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var body = $"Förmedling {request.Ranking.Broker.Name} har avbokat tolkuppdraget med boknings-ID {orderNumber} med meddelande:\n{request.CancelMessage}";
            CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har avbokat tolkuppdraget med boknings-ID {orderNumber}",
                body + GoToOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId));
        }

        public void RequestReplamentOrderAccepted(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestReplamentOrderAccepted), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    var body = $"Svar på ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Ersättningsuppdrag har accepterats. {requireApprovementText}";
                    CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        body + GoToOrderPlain(request.Order.OrderId),
                        HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId));
                    break;
                case RequestStatus.Approved:
                    var bodyAppr = $"Ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har accepteras. Inga förändrade krav finns, tolkuppdrag är klart för utförande.";
                    CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        bodyAppr + GoToOrderPlain(request.Order.OrderId, HtmlHelper.ViewTab.Default, true),
                        HtmlHelper.ToHtmlBreak(bodyAppr) + GoToOrderButton(request.Order.OrderId, HtmlHelper.ViewTab.Default, null, true, true));
                    NotifyBrokerOnAcceptedAnswer(request, orderNumber);
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestReplamentOrderAccepted)} cannot send notifications on requests with status: {request.Status.ToString()}");
            }
        }

        public void RequestReplamentOrderDeclinedByBroker(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestReplamentOrderDeclinedByBroker), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;

            var body = $"Svar på ersättningsuppdrag {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} " +
                $"har tackat nej till ersättningsuppdrag med följande meddelande:\n{request.DenyMessage}";

            CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har tackat nej till ersättningsuppdrag {orderNumber}",
                $"{body} {GoToOrderPlain(request.Order.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderButton(request.Order.OrderId)}");
        }

        public void RequestChangedInterpreter(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestChangedInterpreter), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;

            var body = $"Nytt svar på bokningsförfrågan med boknings-ID {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk för uppdraget.\n\n" +
                OrderReferenceNumberInfo(request) +
                $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                $"{InterpreterCompetenceInfo(request.CompetenceLevel)}\n\n" +
                (request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved ?
                    requireApprovementText :
                    "Inga förändrade krav finns, bokningsförfrågan behåller sin nuvarande status.") +
                    GetPossibleInfoNotValidatedInterpreter(request);
            CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har bytt tolk för uppdrag med boknings-ID {orderNumber}",
                $"{body} {GoToOrderPlain(request.Order.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderButton(request.Order.OrderId)}");
        }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestChangedInterpreterAccepted), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            //Broker
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestReplacedInterpreterAccepted, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Byte av tolk godkänt för boknings-ID {orderNumber}",
                $"Bytet av tolk har godkänts för tolkuppdrag med boknings-ID {orderNumber}. {GoToRequestPlain(request.RequestId)}",
                $"Bytet av tolk har godkänts för tolkuppdrag med boknings-ID {orderNumber}. {GoToRequestButton(request.RequestId)}",
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
            //Creator
            switch (changeOrigin)
            {
                case InterpereterChangeAcceptOrigin.NoNeedForUserAccept:
                    var bodyNoAccept = $"Nytt svar på bokningsförfrågan {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk för uppdraget.\n\n" +
                        OrderReferenceNumberInfo(request) +
                        $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                        $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                        $"{InterpreterCompetenceInfo(request.CompetenceLevel)}\n\n" +
                        "Inga förändrade krav finns, bokningsförfrågan behåller sin nuvarande status." +
                        GetPossibleInfoNotValidatedInterpreter(request);
                    CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har bytt tolk för uppdrag med boknings-ID {orderNumber}",
                        $"{bodyNoAccept} {GoToOrderPlain(request.Order.OrderId, HtmlHelper.ViewTab.Default, true)}",
                        $"{HtmlHelper.ToHtmlBreak(bodyNoAccept)} {GoToOrderButton(request.Order.OrderId, HtmlHelper.ViewTab.Default, null, true, true)}"
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
            NullCheckHelper.ArgumentCheckNull(request, nameof(RemindUnhandledRequest), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            string body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} väntar på hantering. Bokningsförfrågan har "
            + (request.Status == RequestStatus.AcceptedNewInterpreterAppointed ? "ändrats med ny tolk. " : "accepterats. ")
            + requireApprovementText;

            CreateEmail(GetRecipientsFromOrder(request.Order), $"Bokningsförfrågan {orderNumber} väntar på hantering",
                body + GoToOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId));
        }

        public void PartialRequestGroupAnswerAccepted(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(PartialRequestGroupAnswerAccepted), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            Order order = requestGroup.OrderGroup.FirstOrder;
            var body = $"Svar på sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Del av bokningsförfrågan har accepterats.\n" +
                $"Den extra tolk som avropades har gått vidare som en egen förfrågan till nästa förmedling. {requireApprovementText}\n\n" +
                $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                $"\tTillfällen: \n" +
                $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
            CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                $"Förmedling har delvis accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId));
        }

        public void PartialRequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(PartialRequestGroupAnswerAutomaticallyApproved), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            Order order = requestGroup.OrderGroup.FirstOrder;

            var body = $"Svar på sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Del av bokningsförfrågan har accepterats.\n\n" +
                $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                $"\tTillfällen: \n" +
                $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
            CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                $"Förmedling har delvis accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId));
            NotifyBrokerOnAcceptedAnswer(requestGroup, orderGroupNumber);
        }

        public void CreateEmail(string recipient, string subject, string plainBody, string htmlBody = null, bool isBrokerMail = false, bool addContractInfo = true)
        {
            CreateEmail(new[] { recipient }, subject, plainBody, string.IsNullOrEmpty(htmlBody) ? HtmlHelper.ToHtmlBreak(plainBody) : htmlBody, isBrokerMail, addContractInfo);
        }

        public void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, int replacingEmailId, int resentByUserId)
        {
            _dbContext.Add(new OutboundEmail(
                    recipient,
                    subject,
                    plainBody,
                    htmlBody,
                    _clock.SwedenNow,
                    replacingEmailId,
                    resentByUserId));
            _dbContext.SaveChanges();
        }

        public void NotifyOnFailure(int callId)
        {
            OutboundWebHookCall call = _dbContext.OutboundWebHookCalls
                .Include(c => c.RecipientUser)
                .Single(c => c.OutboundWebHookCallId == callId);
            var email = GetBrokerNotificationSettings(call.RecipientUser.BrokerId.Value, NotificationType.ErrorNotification, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ett webhook-anrop från {Constants.SystemName} har misslyckats fem gånger",
                $@"Webhook misslyckades av typ: {call.NotificationType.GetDescription()}({call.NotificationType.GetCustomName()})
{GoToWebHookListPlain()}",
                $@"Webhook misslyckades av typ: {call.NotificationType.GetDescription()}({call.NotificationType.GetCustomName()})<br/>
{GoToWebHookListButton("Gå till systemets loggsida")}",
                true
            );
            }
            var webhook = GetBrokerNotificationSettings(call.RecipientUser.BrokerId.Value, NotificationType.ErrorNotification, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new ErrorMessageModel
                    {
                        ReportedAt = _clock.SwedenNow,
                        CallId = callId,
                        NotificationType = call.NotificationType.GetCustomName()
                    },
                    webhook.ContactInformation,
                    NotificationType.ErrorNotification,
                    webhook.RecipientUserId
                );
            }
            if (_tolkBaseOptions.Support.ReportWebHookFailures)
            {
                CreateEmail(
                    _tolkBaseOptions.Support.SecondLineEmail,
                    "Ett webhook-anrop har misslyckats fem gånger",
                    $@"Webhook misslyckades av typ: {call.NotificationType.GetDescription()}({call.NotificationType.GetCustomName()}) till brokerId:{call.RecipientUser.BrokerId.Value}"
                );
            }
        }

        public bool ResendWebHook(OutboundWebHookCall failedCall, int? resentUserId = null, int? resentImpersonatorUserId = null)
        {
            NullCheckHelper.ArgumentCheckNull(failedCall, nameof(ResendWebHook), nameof(NotificationService));

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
                webhook.RecipientUserId,
                resentUserId,
                resentImpersonatorUserId);

            _dbContext.OutboundWebHookCalls.Add(newCall);
            _dbContext.SaveChanges();
            failedCall.ResentHookId = newCall.OutboundWebHookCallId;
            _dbContext.SaveChanges();

            return true;
        }

        //SHOULD PROBABLY NOT BE HERE AT ALL...
        public void FlushNotificationSettings()
        {
            _cache.Remove(brokerSettingsCacheKey);
        }

        private string GetPossibleInfoNotValidatedInterpreter(Request request)
        {
            var shouldCheckValidationCode = _tolkBaseOptions.Tellus.IsActivated && request.InterpreterCompetenceVerificationResultOnAssign.HasValue;
            bool isInterpreterValidationError = shouldCheckValidationCode && (request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.UnknownError || request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.ConnectionError);
            bool isInterpreterVerified = request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.Validated;
            return isInterpreterValidationError ? "\n\nObservera att tolkens kompetensnivå inte har gått att kontrollera mot Kammarkollegiets tolkregister pga att det inte gick att nå tolkregistret. Risk finns att ställda krav på kompetensnivå inte uppfylls. Mer information finns i Kammarkollegiets tolkregister." : (shouldCheckValidationCode && !isInterpreterVerified) ? "\n\nObservera att tillsatt tolk för tolkuppdraget inte finns registrerad i Kammarkollegiets tolkregister med kravställd/önskad kompetensnivå för detta språk. Risk finns att ställda krav på kompetensnivå inte uppfylls. Mer information finns i Kammarkollegiets tolkregister." : string.Empty;
        }

        private void CreateEmail(IEnumerable<string> recipients, string subject, string plainBody, string htmlBody, bool isBrokerMail = false, bool addContractInfo = true)
        {
            string noReply = "Detta e-postmeddelande går inte att svara på.";
            string handledBy = $"Detta ärende hanteras i {Constants.SystemName}.";
            string contractInfo = "Avrop från ramavtal för tolkförmedlingstjänster 23.3-9066-16";

            foreach (string recipient in recipients)
            {
                _dbContext.Add(new OutboundEmail(
                    recipient,
                    _senderPrepend + subject,
                    $"{plainBody}\n\n{noReply}" + (isBrokerMail ? $"\n\n{handledBy}" : "") + (addContractInfo ? $"\n\n{contractInfo}" : ""),
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
                DisplayPriceInformation priceInfo = PriceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList());
                string invoiceInfo = string.Empty;
                invoiceInfo += $"Följande tolktaxa har använts för beräkning: {priceInfo.PriceListTypeDescription} {priceInfo.CompetencePriceDescription}\n\n";
                foreach (DisplayPriceRow dpr in priceInfo.DisplayPriceRows)
                {
                    invoiceInfo += $"{dpr.Description}:\n{dpr.Price.ToSwedishString("#,0.00 SEK")}\n\n";
                }
                invoiceInfo += $"Total summa: {priceInfo.TotalPrice.ToSwedishString("#,0.00 SEK")}";
                return invoiceInfo;
            }
        }

        private void NotifyBrokerOnAcceptedAnswer(Request request, string orderNumber)
        {
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerApproved, NotificationChannel.Email);
            if (email != null)
            {
                var body = $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av tolk på bokningsförfrågan {orderNumber}.";
                CreateEmail(email.ContactInformation, $"Tolkuppdrag med boknings-ID {orderNumber} verifierat",
                        body + GoToRequestPlain(request.RequestId),
                        body + GoToRequestButton(request.RequestId),
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

        private void NotifyBrokerOnAcceptedAnswer(RequestGroup requestGroup, string orderGroupNumber)
        {
            var email = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerApproved, NotificationChannel.Email);
            if (email != null)
            {
                var body = $"{requestGroup.OrderGroup.CustomerOrganisation.Name} har godkänt tillsättningen av tolk på den sammanhållna bokningsförfrågan {orderGroupNumber}.";
                CreateEmail(email.ContactInformation, $"Sammanhållen bokning med boknings-ID {orderGroupNumber} verifierat",
                        body + GoToRequestGroupPlain(requestGroup.RequestGroupId),
                        body + GoToRequestGroupButton(requestGroup.RequestGroupId),
                        true);
            }
            var webhook = GetBrokerNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerApproved, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestGroupAnswerApprovedModel
                    {
                        OrderGroupNumber = orderGroupNumber
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestGroupAnswerApproved,
                    webhook.RecipientUserId
                );
            }
        }

        private static IEnumerable<string> GetRecipientsFromOrder(Order order, bool sendToContactPerson = false)
        {
            yield return order.ContactEmail;
            if (sendToContactPerson && order.ContactPersonId.HasValue)
            {
                yield return order.ContactPersonUser.Email;
            }
        }

        private static IEnumerable<string> GetRecipientsFromOrderGroup(OrderGroup orderGroup)
        {
            var unit = orderGroup.CustomerUnit;
            yield return unit != null ? unit.Email : orderGroup.CreatedByUser.Email;
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

        private void CreateWebHookCall(WebHookPayloadBaseModel payload, string recipientUrl, NotificationType type, int userId)
        {
            _dbContext.Add(new OutboundWebHookCall(
                recipientUrl,
                payload.AsJson(),
                type,
                _clock.SwedenNow,
                userId));
            _dbContext.SaveChanges();
        }

        private string GoToOrderPlain(int orderId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, bool enableOrderPrint = false)
        {
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    string printInfo = enableOrderPrint ? $"\n\nSkriv ut bokningsbekräftelse: {HtmlHelper.GetOrderPrintUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}" : string.Empty;
                    return $"\n\n\nGå till bokning: {HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId)}{printInfo}";
                case HtmlHelper.ViewTab.Requisition:
                    return $"\n\n\nGå till rekvisition: {HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId, "tab=requisition")}";
                case HtmlHelper.ViewTab.Complaint:
                    return $"\n\n\nGå till reklamation: {HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId, "tab=complaint")}";
            }
        }

        private string GoToOrderGroupPlain(int orderGroupId) => $"\n\n\nGå till sammanhållen bokning: {HtmlHelper.GetOrderGroupViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderGroupId)}";

        private string GoToRequestPlain(int requestId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default)
        {
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return $"\n\n\nGå till bokningsförfrågan: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}";
                case HtmlHelper.ViewTab.Requisition:
                    return $"\n\n\nGå till rekvisition: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=requisition")}";
                case HtmlHelper.ViewTab.Complaint:
                    return $"\n\n\nGå till reklamation: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=complaint")}";
            }
        }

        private string GoToRequestGroupPlain(int requestGroupId) => $"\n\n\nGå till bokningsförfrågan: {HtmlHelper.GetRequestGroupViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestGroupId)}";

        private string GoToOrderButton(int orderId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, string textOverride = null, bool autoBreakLines = true, bool enableOrderPrint = false)
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
                    string printInfo = enableOrderPrint ? $"<br /><br />{HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderPrintUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId), "Skriv ut bokningsbekräftelse")}" : string.Empty;
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId), "Till bokning") + printInfo;
                case HtmlHelper.ViewTab.Requisition:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId, "tab=requisition"), "Till rekvisition");
                case HtmlHelper.ViewTab.Complaint:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderId, "tab=complaint"), "Till reklamation");
            }
        }

        private string GoToOrderGroupButton(int orderGroupId) => $"<br /><br /><br /> {HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderGroupViewUrl(_tolkBaseOptions.TolkWebBaseUrl, orderGroupId), "Till bokning")}";

        private string GoToRequestButton(int requestId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, string textOverride = null, bool autoBreakLines = true)
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
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=requisition"), "Till rekvisition");
                case HtmlHelper.ViewTab.Complaint:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=complaint"), "Till reklamation");
            }
        }

        private string GoToRequestGroupButton(int requestGroupId, string textOverride = null, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestGroupViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestGroupId), textOverride ?? "Till bokning");
        }

        private static RequestModel GetRequestModel(Request request)
        {
            var order = request.Order;

            return new RequestModel
            {
                CreatedAt = request.CreatedAt,
                OrderNumber = order.OrderNumber,
                CustomerInformation = new CustomerInformationModel
                {
                    Name = order.CustomerOrganisation.Name,
                    Key = order.CustomerOrganisation.OrganisationPrefix,
                    OrganisationNumber = order.CustomerOrganisation.OrganisationNumber,
                    ContactName = request.Order.CreatedByUser.FullName,
                    ContactPhone = request.Order.ContactPhone,
                    ContactEmail = request.Order.ContactEmail,
                    InvoiceReference = order.InvoiceReference,
                    PriceListType = order.CustomerOrganisation.PriceListType.GetCustomName(),
                    TravelCostAgreementType = order.CustomerOrganisation.TravelCostAgreementType.GetCustomName(),
                    ReferenceNumber = order.CustomerReferenceNumber,
                    UnitName = order.CustomerUnit?.Name,
                    DepartmentName = order.UnitName
                },
                //D2 pads any single digit with a zero 1 -> "01"
                Region = order.Region.RegionId.ToSwedishString("D2"),
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
                    OffsiteContactInformation = l.OffSiteContactInformation,
                    Street = l.Street,
                    City = l.City,
                    Rank = l.Rank,
                    Key = EnumHelper.GetCustomName(l.InterpreterLocation)
                }),
                CompetenceLevels = order.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = EnumHelper.GetCustomName(c.CompetenceLevel),
                    Rank = c.Rank ?? 0
                }),
                AllowMoreThanTwoHoursTravelTime = order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                CreatorIsInterpreterUser = order.CreatorIsInterpreterUser,
                AssignmentType = EnumHelper.GetCustomName(order.AssignmentType),
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
                PriceInformation = order.PriceRows.GetPriceInformationModel(order.PriceCalculatedFromCompetenceLevel.GetCustomName(), request.Ranking.BrokerFee)
            };
        }

        private string GoToWebHookListPlain() => $"\n\n\nGå till systemets loggsida för att få mer information : {HtmlHelper.GetWebHookListUrl(_tolkBaseOptions.TolkWebBaseUrl)}";

        private string GoToWebHookListButton(string textOverride, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetWebHookListUrl(_tolkBaseOptions.TolkWebBaseUrl), textOverride);
        }

        private static RequestGroupModel GetRequestGroupModel(RequestGroup requestGroup)
        {
            var orderGroup = requestGroup.OrderGroup;
            var order = orderGroup.FirstOrder;
            return new RequestGroupModel
            {
                CreatedAt = requestGroup.CreatedAt,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                Customer = orderGroup.CustomerOrganisation.Name,
                CustomerOrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                //D2 pads any single digit with a zero 1 -> "01"
                Region = orderGroup.Region.RegionId.ToSwedishString("D2"),
                Language = new LanguageModel
                {
                    Key = orderGroup.Language?.ISO_639_Code,
                    Description = orderGroup.OtherLanguage ?? orderGroup.Language.Name,
                },
                ExpiresAt = requestGroup.ExpiresAt,
                Locations = order.InterpreterLocations.Select(l => new LocationModel
                {
                    OffsiteContactInformation = l.OffSiteContactInformation,
                    Street = l.Street,
                    City = l.City,
                    Rank = l.Rank,
                    Key = EnumHelper.GetCustomName(l.InterpreterLocation)
                }),
                CompetenceLevels = orderGroup.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = EnumHelper.GetCustomName(c.CompetenceLevel),
                    Rank = c.Rank ?? 0
                }),
                AllowExceedingTravelCost = orderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                CreatorIsInterpreterUser = orderGroup.CreatorIsInterpreterUser,
                AssignmentType = EnumHelper.GetCustomName(orderGroup.AssignmentType),
                Description = order.Description,
                CompetenceLevelsAreRequired = orderGroup.SpecificCompetenceLevelRequired,
                Requirements = orderGroup.Requirements.Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderGroupRequirementId,
                    RequirementType = EnumHelper.GetCustomName(r.RequirementType)
                }),
                Attachments = orderGroup.Attachments.Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.Attachment.FileName
                }),
                Occasions = orderGroup.Orders.Select(o => new OccasionModel
                {
                    OrderNumber = o.OrderNumber,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    IsExtraInterpreterForOrderNumber = o.IsExtraInterpreterForOrder?.OrderNumber,
                    PriceInformation = o.PriceRows.GetPriceInformationModel(o.PriceCalculatedFromCompetenceLevel.GetCustomName(), requestGroup.Ranking.BrokerFee)
                })
            };
        }

        private Request GetRequest(int id)
        {
            return _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(o => o.Attachment)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Single(o => o.RequestId == id);
        }
    }
}
