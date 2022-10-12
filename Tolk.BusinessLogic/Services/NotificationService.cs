using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly CacheService _cacheService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ITolkBaseOptions _tolkBaseOptions;
        private readonly string _senderPrepend;

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock,
            CacheService cacheService,
            PriceCalculationService priceCalculationService,
            ITolkBaseOptions tolkBaseOptions
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _cacheService = cacheService;
            _priceCalculationService = priceCalculationService;
            _tolkBaseOptions = tolkBaseOptions;
            _senderPrepend = !string.IsNullOrWhiteSpace(_tolkBaseOptions?.Env.DisplayName) ? $"{_tolkBaseOptions?.Env.DisplayName} " : string.Empty;
        }

        public void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(OrderCancelledByCustomer), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;

            //customer send email with info about requisition created
            NotificationType notificationType = NotificationType.RequestCancelledByCustomerWhenApproved;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
                {
                    string body = $"Rekvisition har skapats pga att myndigheten har avbokat uppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(request)}. Uppdraget avbokades med detta meddelande:\n{request.CancelMessage}\n" +
                         (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, i de fall något ersättningsuppdrag inte kan ordnas av kund. Observera att ersättning kan tillkomma för eventuell tidsspillan som tolken skulle behövt ta ut för genomförande av aktuellt uppdrag. Även kostnader avseende resor och boende som ej är avbokningsbara, alternativt avbokningskostnader för resor och boende som avbokats kan tillkomma. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna."
                         : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.");
                    CreateEmail(GetRecipientsFromOrder(request.Order, true), $"Rekvisition har skapats pga avbokat uppdrag boknings-ID {orderNumber}",
                        body + GoToOrderPlain(request.Order.OrderId),
                        HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                        notificationType,
                        true);
                }
            }
            //broker
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCancelledByCustomer, NotificationChannel.Email);
            if (email != null)
            {
                if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
                {
                    string body = $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                         $"Uppdraget har boknings-ID {orderNumber}{RequestReferenceNumberInfo(request)} och skulle ha startat {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}." +
                         (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, i de fall något ersättningsuppdrag inte kan ordnas av kund. Observera att ersättning kan tillkomma för eventuell tidsspillan som tolken skulle behövt ta ut för genomförande av aktuellt uppdrag. Även kostnader avseende resor och boende som ej är avbokningsbara, alternativt avbokningskostnader för resor och boende som avbokats kan tillkomma. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.");
                    CreateEmail(email.ContactInformation, $"Avbokat tolkuppdrag boknings-ID {orderNumber}",
                        body + GoToRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(request.RequestId),
                        NotificationType.RequestCancelledByCustomerWhenApproved,
                        true);
                }
                else
                {
                    var body = $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                        $"Uppdraget har boknings-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}.";
                    CreateEmail(email.ContactInformation, $"Avbokad förfrågan boknings-ID {orderNumber}",
                        body + GoToRequestPlain(request.RequestId),
                        HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(request.RequestId),
                        NotificationType.RequestCancelledByCustomer,
                        true);
                }
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCancelledByCustomer, NotificationChannel.Webhook);
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

        public void OrderGroupCancelledByCustomer(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(OrderGroupCancelledByCustomer), nameof(NotificationService));
            string orderNumber = requestGroup.OrderGroup.OrderGroupNumber;
            //notify broker
            var email = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupCancelledByCustomer, NotificationChannel.Email);
            if (email != null)
            {
                var occasionText = requestGroup.OrderGroup.IsSingleOccasion ? "Tillfället skulle ha startat " : "Första tillfället skulle ha startat ";
                var body = $"Sammanhållen bokningsförfrågan med boknings-ID {orderNumber}{RequestReferenceNumberInfo(requestGroup)} från {requestGroup.OrderGroup.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{requestGroup.CancelMessage}\n" +
                     $"{occasionText + requestGroup.OrderGroup.FirstOrder.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}.";

                CreateEmail(email.ContactInformation, $"Avbokad sammanhållen bokningsförfrågan boknings-ID {orderNumber}",
                    body + GoToRequestGroupPlain(requestGroup.RequestGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToRequestGroupButton(requestGroup.RequestGroupId),
                    NotificationType.RequestGroupCancelledByCustomer,
                    true);
            }
            var webhook = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupCancelledByCustomer, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                   new RequestGroupCancelledByCustomerModel
                   {
                       OrderGroupNumber = orderNumber,
                       Message = requestGroup.CancelMessage
                   },
                   webhook.ContactInformation,
                   NotificationType.RequestGroupCancelledByCustomer,
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
            NotificationType notificationType = NotificationType.RequisitionApprovalRightsRemoved;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType) 
                && !string.IsNullOrEmpty(previousContactUser?.Email))
                {
                    string body = $"Behörighet att granska rekvisition har ändrats. Du har inte längre denna behörighet för bokning {orderNumber}.";
                    CreateEmail(previousContactUser.Email, subject,
                        body + GoToOrderPlain(order.OrderId),
                        HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(order.OrderId),
                        notificationType);
                }
            notificationType = NotificationType.RequisitionApprovalRightsAdded;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType)
            && !string.IsNullOrEmpty(currentContactUser?.Email))
            {
                string body = $"Behörighet att granska rekvisition har ändrats. Du har nu behörighet att utföra detta för bokning {orderNumber}.";
                CreateEmail(currentContactUser.Email, subject,
                    body + GoToOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(order.OrderId),
                    notificationType);
            }
        }

        public void OrderUpdated(Order order, bool attachmentChanged, bool orderFieldsUpdated)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(OrderUpdated), nameof(NotificationService));

            var request = order.Requests.OrderBy(r => r.RequestId).Last();
            var orderNumber = order.OrderNumber;

            var lastEntry = orderFieldsUpdated ? order.OrderChangeLogEntries.OrderBy(oc => oc.OrderChangeLogEntryId)
                .Last(o => o.OrderChangeLogType == OrderChangeLogType.OrderInformationFields || o.OrderChangeLogType == OrderChangeLogType.AttachmentAndOrderInformationFields) : null;
            //get the interpreterlocation from request to get the correct string from order.InterpreterLocations to compare to
            var interpreterLocation = (InterpreterLocation)request.InterpreterLocation.Value;

            string interpreterLocationText = interpreterLocation == InterpreterLocation.OffSitePhone || interpreterLocation == InterpreterLocation.OffSiteVideo ?
                order.InterpreterLocations.Where(i => i.InterpreterLocation == interpreterLocation).Single().OffSiteContactInformation :
                order.InterpreterLocations.Where(i => i.InterpreterLocation == interpreterLocation).Single().Street;

            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestInformationUpdated, NotificationChannel.Email);
            if (email != null)
            {
                var attachmentText = attachmentChanged ? "Bifogade filer har ändrats.\n\n" : string.Empty;
                var orderFieldText = orderFieldsUpdated ? GetOrderChangeText(order, lastEntry, interpreterLocationText) : string.Empty;
                string body = $"Ert tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(request)} hos {request.Order.CustomerOrganisation.Name} har uppdaterats med ny information.\n\n {attachmentText} {orderFieldText}\nKlicka på länken nedan för att se det uppdaterade tolkuppdraget i sin helhet.";
                CreateEmail(email.ContactInformation, $"Tolkuppdrag med boknings-ID {orderNumber} har uppdaterats",
                    body + GoToRequestPlain(request.RequestId),
                    HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(request.RequestId),
                    NotificationType.RequestInformationUpdated,
                    true);
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestInformationUpdated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    GetRequestUpdatedModel(order, attachmentChanged, orderFieldsUpdated, lastEntry, interpreterLocation, interpreterLocationText),
                    webhook.ContactInformation,
                    NotificationType.RequestInformationUpdated,
                    webhook.RecipientUserId
                );
            }
        }

        public async Task OrderReplacementCreated(int replacedRequestId, int newRequestId)
        {
            Request oldRequest = await _dbContext.Requests.GetRequestById(replacedRequestId);

            Request replacementRequest = await _dbContext.Requests.GetRequestById(newRequestId);
            var email = GetOrganisationNotificationSettings(replacementRequest.Ranking.BrokerId, NotificationType.RequestReplacementCreated, NotificationChannel.Email);
            if (email != null)
            {
                var bodyPlain = $"\tOrginal Start: {oldRequest.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tOrginal Slut: {oldRequest.Order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tErsättning Start: {replacementRequest.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tErsättning Slut: {replacementRequest.Order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                   $"\tSvara senast: {replacementRequest.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}" +
                   $"Gå till ersättningsuppdrag: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, replacementRequest.RequestId)}\n" +
                   $"Gå till ursprungligt uppdrag: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, oldRequest.RequestId)}";
                var bodyHtml = $@"
<ul>
<li>Orginal Start: {oldRequest.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Orginal Slut: {oldRequest.Order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Start: {replacementRequest.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Slut: {replacementRequest.Order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
<li>Svara senast: {replacementRequest.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{GoToRequestButton(replacementRequest.RequestId, textOverride: "Gå till ersättningsuppdrag", autoBreakLines: false)}</div>
<div>{GoToRequestButton(oldRequest.RequestId, textOverride: "Gå till ursprungligt uppdrag", autoBreakLines: false)}</div>";
                CreateEmail(
                     email.ContactInformation,
                     $"Bokning {oldRequest.Order.OrderNumber} har avbokats, med ersättningsuppdrag: {replacementRequest.Order.OrderNumber}",
                     bodyPlain,
                     bodyHtml,
                     NotificationType.RequestReplacementCreated,
                     true);
            }
            var webhook = GetOrganisationNotificationSettings(replacementRequest.Ranking.BrokerId, NotificationType.RequestReplacementCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestReplacementCreatedModel
                    {
                        OriginalRequest = await GetRequestModel(oldRequest),
                        ReplacementRequest = await GetRequestModel(await GetRequest(replacementRequest.RequestId))
                    },
                   webhook.ContactInformation,
                   NotificationType.RequestReplacementCreated,
                   webhook.RecipientUserId
               );
            }
        }

        public void OrderTerminated(Order order)
        {
            NullCheckHelper.ArgumentCheckNull(order, nameof(OrderTerminated), nameof(NotificationService));
            var body = GetOrderTerminatedText(order.Status, order.OrderNumber);
            CreateEmail(GetRecipientsFromOrder(order),
                $"Bokningsförfrågan {order.OrderNumber} fick ingen bekräftad tolktillsättning",
                body + GoToOrderPlain(order.OrderId),
                body + GoToOrderButton(order.OrderId),
                NotificationType.OrderTerminated
            );
        }

        public void OrderGroupTerminated(OrderGroup terminatedOrderGroup)
        {
            NullCheckHelper.ArgumentCheckNull(terminatedOrderGroup, nameof(OrderGroupTerminated), nameof(NotificationService));
            NotificationType notificationType = NotificationType.OrderGroupTerminated;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                var body = GetOrderGroupTerminatedText(terminatedOrderGroup.Status, terminatedOrderGroup.OrderGroupNumber);
                CreateEmail(GetRecipientsFromOrderGroup(terminatedOrderGroup),
                    $"Sammanhållen bokningsförfrågan {terminatedOrderGroup.OrderGroupNumber} fick ingen bekräftad tolktillsättning",
                    body + GoToOrderGroupPlain(terminatedOrderGroup.OrderGroupId),
                    body + GoToOrderGroupButton(terminatedOrderGroup.OrderGroupId),
                    notificationType
                );
            }
        }

        public async Task RequestCreated(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCreated), nameof(NotificationService));
            var order = request.Order;
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Email);
            if (email != null)
            {
                string bodyPlain = $"Bokningsförfrågan för tolkuppdrag {order.OrderNumber} från {order.CustomerOrganisation.Name} har inkommit via {Constants.SystemName}.\nMyndighetens organisationsnummer: {order.CustomerOrganisation.OrganisationNumber}\nMyndighetens Peppol-ID: {order.CustomerOrganisation.PeppolId}\n\nObservera att bekräftelse måste lämnas via avropstjänsten.\n\n" +
                    $"\tUppdragstyp: {EnumHelper.GetDescription(order.AssignmentType)}\n" +
                    $"\tRegion: {order.Region.Name}\n" +
                    $"\tSpråk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tStart: {order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSlut: {order.EndAt.ToSwedishString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSvara senast: {request.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}" +
                    GoToRequestPlain(request.RequestId);

                string bodyHtml = $@"Bokningsförfrågan för tolkuppdrag {order.OrderNumber} från {order.CustomerOrganisation.Name} har inkommit via {Constants.SystemName}.<br />Myndighetens organisationsnummer: {order.CustomerOrganisation.OrganisationNumber}<br />Myndighetens Peppol-ID: {order.CustomerOrganisation.PeppolId}<br /><br />Observera att bekräftelse måste lämnas via avropstjänsten.<br />
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
                    NotificationType.RequestCreated,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    await GetRequestModel(request),
                    webhook.ContactInformation,
                    NotificationType.RequestCreated,
                    webhook.RecipientUserId
                );
            }
        }

        public async Task RequestGroupCreated(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupCreated), nameof(NotificationService));
            var orderGroup = requestGroup.OrderGroup;
            var email = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupCreated, NotificationChannel.Email);
            if (email != null)
            {
                string bodyPlain = $"Bokningsförfrågan för ett sammanhållet tolkuppdrag {orderGroup.OrderGroupNumber} från {orderGroup.CustomerOrganisation.Name} har inkommit via {Constants.SystemName}.\nMyndighetens organisationsnummer: {orderGroup.CustomerOrganisation.OrganisationNumber}\nMyndighetens Peppol-ID: {orderGroup.CustomerOrganisation.PeppolId}\n\nObservera att bekräftelse måste lämnas via avropstjänsten.\n\n" +
                    $"\tUppdragstyp: {EnumHelper.GetDescription(orderGroup.AssignmentType)}\n" +
                    $"\tRegion: {orderGroup.Region.Name}\n" +
                    $"\tSpråk: {orderGroup.LanguageName}\n" +
                    $"\tTillfällen: \n" +
                    $"{GetOccurences(orderGroup.Orders)}\n" +
                    $"\tSvara senast: {requestGroup.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}" +
                    GoToRequestGroupPlain(requestGroup.RequestGroupId);
                string bodyHtml = $@"Bokningsförfrågan för ett sammanhållet tolkuppdrag {orderGroup.OrderGroupNumber} från {orderGroup.CustomerOrganisation.Name} har inkommit via {Constants.SystemName}.<br />Myndighetens organisationsnummer: {orderGroup.CustomerOrganisation.OrganisationNumber}<br />Myndighetens Peppol-ID: {orderGroup.CustomerOrganisation.PeppolId}<br /><br />Observera att bekräftelse måste lämnas via avropstjänsten.<br />
                    <ul>
                    <li>Uppdragstyp: {EnumHelper.GetDescription(orderGroup.AssignmentType)}</li>
                    <li>Region: {orderGroup.Region.Name}</li>
                    <li>Språk: {orderGroup.LanguageName}</li>
                    <li>Tillfällen:{GetOccurencesAsHtmlList(orderGroup.Orders)}</li>
                    <li>Svara senast: {requestGroup.ExpiresAt?.ToSwedishString("yyyy-MM-dd HH:mm")}</li>
                    </ul>
                    <div>{GoToRequestGroupButton(requestGroup.RequestGroupId, textOverride: "Till förfrågan", autoBreakLines: false)}</div>";
                CreateEmail(
                    email.ContactInformation,
                    $"Ny sammanhållen bokningsförfrågan registrerad: {orderGroup.OrderGroupNumber}",
                    bodyPlain,
                    bodyHtml,
                    NotificationType.RequestGroupCreated,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    await GetRequestGroupModel(requestGroup),
                    webhook.ContactInformation,
                    NotificationType.RequestGroupCreated,
                    webhook.RecipientUserId
                );
            }
        }

        public void RequestCreatedWithoutExpiry(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCreatedWithoutExpiry), nameof(NotificationService));
            NotificationType notificationType = NotificationType.RequestCreatedWithoutExpiry;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string body = $@"Bokningsförfrågan {request.Order.OrderNumber} måste kompletteras med sista svarstid innan den kan skickas till nästa förmedling för tillsättning.

Notera att er förfrågan INTE skickas vidare till nästa förmedling, tills dess sista svarstid är satt.";

                CreateEmail(GetRecipientsFromOrder(request.Order),
                    $"Sista svarstid ej satt på bokningsförfrågan {request.Order.OrderNumber}",
                    $"{body} {GoToOrderPlain(request.OrderId)}",
                    $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderButton(request.OrderId)}",
                    notificationType
                );
                _logger.LogInformation($"Email created for customer regarding missing expiry on request {request.RequestId} for order {request.OrderId}");
            }
        }

        public void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(newRequestGroup, nameof(RequestGroupCreatedWithoutExpiry), nameof(NotificationService));
            NotificationType notificationType = NotificationType.RequestgroupCreatedWithoutExpiry;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderGroupNumber = newRequestGroup.OrderGroup.OrderGroupNumber;
                string body = $@"Sammanhållen bokningsförfrågan {orderGroupNumber} måste kompletteras med sista svarstid innan den kan skickas till nästa förmedling för tillsättning.

Notera att er förfrågan INTE skickas vidare till nästa förmedling, tills dess sista svarstid är satt.";

                CreateEmail(GetRecipientsFromOrderGroup(newRequestGroup.OrderGroup),
                    $"Sista svarstid ej satt på sammanhållen bokningsförfrågan {orderGroupNumber}",
                    $"{body} {GoToOrderGroupPlain(newRequestGroup.OrderGroup.OrderGroupId)}",
                    $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderGroupButton(newRequestGroup.OrderGroup.OrderGroupId)}",
                    notificationType
                );
                _logger.LogInformation($"Email created for customer regarding missing expiry on request group {newRequestGroup.RequestGroupId} for order group {newRequestGroup.OrderGroup.OrderGroupId}");
            }
        }

        public void RequestAnswerAutomaticallyApproved(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAnswerAutomaticallyApproved), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            NotificationType notificationType = NotificationType.OrderAccepted;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                var body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats.\n\n" +
                OrderReferenceNumberInfo(request.Order) +
                $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                InterpreterCompetenceInfo(request.CompetenceLevel) +
                GetPossibleInfoNotValidatedInterpreter(request);

                CreateEmail(GetRecipientsFromOrder(request.Order),
                    $"Förmedling har accepterat bokningsförfrågan {orderNumber}",
                    body + GoToOrderPlain(request.Order.OrderId, HtmlHelper.ViewTab.Default, true),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId, HtmlHelper.ViewTab.Default, null, true, true),
                    notificationType);
            }
            NotifyCustomerOnAcceptedAnswer(request, orderNumber);
            NotifyBrokerOnAcceptedAnswer(request, orderNumber);
        }

        public void RequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupAnswerAutomaticallyApproved), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            NotificationType notificationType = NotificationType.OrderGroupAccepted;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                Order order = requestGroup.OrderGroup.FirstOrder;

                var body = $"Svar på  sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats.\n\n" +
                    OrderReferenceNumberInfo(order) +
                    $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tTillfällen: \n" +
                    $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                    GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
                if (requestGroup.HasExtraInterpreter)
                {
                    body += GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForExtraInterpreter, true);
                }
                CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                    $"Förmedling har accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                    body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId),
                    notificationType
                );
            }
            NotifyBrokerOnAcceptedAnswer(requestGroup, orderGroupNumber);
        }

        public void RequestAnswerApproved(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAnswerApproved), nameof(NotificationService));
            NotifyCustomerOnAcceptedAnswer(request, request.Order.OrderNumber);
            NotifyBrokerOnAcceptedAnswer(request, request.Order.OrderNumber);
        }

        public void RequestAnswerDenied(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestAnswerDenied), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerDenied, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Svar på bokningsförfrågan med boknings-ID {orderNumber} har underkänts",
                    $"Ert svar på bokningsförfrågan {orderNumber}{RequestReferenceNumberInfo(request)} underkändes med följande meddelande:\n{request.DenyMessage}.{GoToRequestPlain(request.RequestId)}",
                    $"Ert svar på bokningsförfrågan {orderNumber}{RequestReferenceNumberInfo(request)} underkändes med följande meddelande:<br />{request.DenyMessage}.{GoToRequestButton(request.RequestId)}",
                    NotificationType.RequestAnswerDenied,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerDenied, NotificationChannel.Webhook);
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
            var email = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerDenied, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Svar på sammanhållen bokningsförfrågan med boknings-ID {orderGroupNumber} har underkänts",
                    $"Ert svar på sammanhållen bokningsförfrågan {orderGroupNumber}{RequestReferenceNumberInfo(requestGroup)} underkändes med följande meddelande:\n{requestGroup.DenyMessage}. {GoToRequestGroupPlain(requestGroup.RequestGroupId)}",
                    $"Ert svar på sammanhållen bokningsförfrågan {orderGroupNumber}{RequestReferenceNumberInfo(requestGroup)} underkändes med följande meddelande:<br />{requestGroup.DenyMessage}. {GoToRequestGroupButton(requestGroup.RequestGroupId)}",
                    NotificationType.RequestGroupAnswerDenied,
                   true
                );
            }
            var webhook = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerDenied, NotificationChannel.Webhook);
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

        public void RequestExpiredDueToInactivity(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestExpiredDueToInactivity), nameof(NotificationService));
            var orderNumber = request.Order.OrderNumber;
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToInactivity, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Bokningsförfrågan {orderNumber} har gått vidare till nästa förmedling i rangordningen",
                    $"Ni har inte besvarat bokningsförfrågan {orderNumber} från {request.Order.CustomerOrganisation.Name} organisationsnummer {request.Order.CustomerOrganisation.OrganisationNumber}.\nTidsfristen enligt ramavtal har nu gått ut. {GoToRequestPlain(request.RequestId)}",
                    $"Ni har inte besvarat bokningsförfrågan {orderNumber} från {request.Order.CustomerOrganisation.Name} organisationsnummer {request.Order.CustomerOrganisation.OrganisationNumber}.<br />Tidsfristen enligt ramavtal har nu gått ut. {GoToRequestButton(request.RequestId)}",
                    NotificationType.RequestLostDueToInactivity,
                    true);
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToInactivity, NotificationChannel.Webhook);
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

        public void RequestExpiredDueToNoAnswerFromCustomer(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestExpiredDueToNoAnswerFromCustomer), nameof(NotificationService));
            var orderNumber = request.Order.OrderNumber;
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToNoAnswerFromCustomer, NotificationChannel.Email);
            if (email != null)
            {
                var mailText = GetRequestExpiredDueToNoAnswerFromCustomerText(request.Order, request);
                CreateEmail(email.ContactInformation,
                    request.LatestAnswerTimeForCustomer.HasValue ? $"Bokningsförfrågan {orderNumber} avslutad, myndigheten svarade inte på tillsättningen i tid" : $"Bokningsförfrågan {orderNumber} avslutad, myndigheten svarade inte på tillsättningen",
                    mailText + GoToRequestPlain(request.RequestId),
                    mailText + GoToRequestButton(request.RequestId),
                    NotificationType.RequestLostDueToNoAnswerFromCustomer,
                    true);
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestLostDueToNoAnswerFromCustomer, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestLostDueToNoAnswerFromCustomerModel
                    {
                        OrderNumber = orderNumber,
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestLostDueToNoAnswerFromCustomer,
                    webhook.RecipientUserId
                );
            }
        }

        public void RequestGroupExpiredDueToInactivity(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupExpiredDueToInactivity), nameof(NotificationService));
            var orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            var email = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupLostDueToInactivity, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Sammanhållen bokningsförfrågan {orderGroupNumber} har gått vidare till nästa förmedling i rangordningen",
                    $"Ni har inte besvarat den sammanhållna bokningsförfrågan {orderGroupNumber} från {requestGroup.OrderGroup.CustomerOrganisation.Name} organisationsnummer {requestGroup.OrderGroup.CustomerOrganisation.OrganisationNumber}.\nTidsfristen enligt ramavtal har nu gått ut. {GoToRequestGroupPlain(requestGroup.RequestGroupId)}",
                    $"Ni har inte besvarat den sammanhållna bokningsförfrågan {orderGroupNumber} från {requestGroup.OrderGroup.CustomerOrganisation.Name} organisationsnummer {requestGroup.OrderGroup.CustomerOrganisation.OrganisationNumber}.<br />Tidsfristen enligt ramavtal har nu gått ut. {GoToRequestGroupButton(requestGroup.RequestGroupId)}",
                    NotificationType.RequestGroupLostDueToInactivity,
                    true);
            }
            var webhook = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupLostDueToInactivity, NotificationChannel.Webhook);
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

        public void RequestGroupExpiredDueToNoAnswerFromCustomer(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupExpiredDueToNoAnswerFromCustomer), nameof(NotificationService));
            var orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            var email = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupLostDueToNoAnswerFromCustomer, NotificationChannel.Email);
            if (email != null)
            {
                var emailText = GetRequestGroupExpiredDueToNoAnswerFromCustomerText(requestGroup.OrderGroup, requestGroup);
                CreateEmail(email.ContactInformation,
                    requestGroup.LatestAnswerTimeForCustomer.HasValue ? $"Sammanhållen bokningsförfrågan {orderGroupNumber} avslutad, myndigheten svarade inte på tillsättningen i tid" :
                    $"Sammanhållen bokningsförfrågan {orderGroupNumber} är avslutad, myndigheten svarade inte på tillsättningen",
                    emailText + GoToRequestGroupPlain(requestGroup.RequestGroupId),
                    emailText + GoToRequestGroupButton(requestGroup.RequestGroupId),
                    NotificationType.RequestGroupLostDueToNoAnswerFromCustomer,
                    true);
            }
            var webhook = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupLostDueToNoAnswerFromCustomer, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestGroupLostDueToNoAnswerFromCustomerModel
                    {
                        OrderGroupNumber = orderGroupNumber,
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestGroupLostDueToNoAnswerFromCustomer,
                    webhook.RecipientUserId
                );
            }
        }

        public void ComplaintCreated(Complaint complaint)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintCreated), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetOrganisationNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintCreated, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"En reklamation har registrerats för tolkuppdrag med boknings-ID {orderNumber}",
                $@"Reklamation för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(complaint.Request)} har skapats.
Reklamationstyp:
{complaint.ComplaintType.GetDescription()}

Angiven reklamationsbeskrivning:
{complaint.ComplaintMessage} 
{GoToRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                $@"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har skapats.<br />Reklamationstyp:<br />
{complaint.ComplaintType.GetDescription()}<br /><br />Angiven reklamationsbeskrivning:<br />
{complaint.ComplaintMessage}
{GoToRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                NotificationType.ComplaintCreated,
                true
            );
            }
            var webhook = GetOrganisationNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintCreated, NotificationChannel.Webhook);
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
            NotificationType notificationType = NotificationType.ComplaintConfirmed;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintConfirmed), nameof(NotificationService));
                string orderNumber = complaint.Request.Order.OrderNumber;
                CreateEmail(complaint.ContactEmail, $"Reklamation kopplad till tolkuppdrag {orderNumber} har godtagits",
                    $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits {GoToOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                    $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har godtagits {GoToOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                    notificationType
                );
            }
        }

        public void ComplaintDisputed(Complaint complaint)
        {
            NotificationType notificationType = NotificationType.ComplaintDisputed;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintDisputed), nameof(NotificationService));
                string orderNumber = complaint.Request.Order.OrderNumber;
                CreateEmail(complaint.ContactEmail, $"Reklamation kopplad till tolkuppdrag {orderNumber} har bestridits",
                    $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage} {GoToOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                    $"Reklamation för tolkuppdrag med boknings-ID {orderNumber} har bestridits med följande meddelande:<br />{complaint.AnswerMessage} {GoToOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                    notificationType
                );
            }
        }

        public void ComplaintDisputePendingTrial(Complaint complaint)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(ComplaintDisputePendingTrial), nameof(NotificationService));
            string orderNumber = complaint.Request.Order.OrderNumber;
            var email = GetOrganisationNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputePendingTrial, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ert bestridande av reklamation avslogs för tolkuppdrag {orderNumber}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(complaint.Request)} har avslagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {GoToRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(complaint.Request)} har avslagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {GoToRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    NotificationType.ComplaintDisputePendingTrial,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputePendingTrial, NotificationChannel.Webhook);
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
            var email = GetOrganisationNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputedAccepted, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ert bestridande av reklamation har godtagits för tolkuppdrag {orderNumber}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(complaint.Request)} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {GoToRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    $"Bestridande av reklamation för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(complaint.Request)} har godtagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {GoToRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                    NotificationType.ComplaintDisputedAccepted,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(complaint.Request.Ranking.BrokerId, NotificationType.ComplaintDisputedAccepted, NotificationChannel.Webhook);
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
            NotificationType notificationType = NotificationType.RequisitionCreated;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                NullCheckHelper.ArgumentCheckNull(requisition, nameof(RequisitionCreated), nameof(NotificationService));
                var order = requisition.Request.Order;
                CreateEmail(GetRecipientsFromOrder(order, true),
                    $"En rekvisition har registrerats för tolkuppdrag {order.OrderNumber}",
                    $"En rekvisition har registrerats för tolkuppdrag med boknings-ID {order.OrderNumber}. {GoToOrderPlain(order.OrderId, HtmlHelper.ViewTab.Requisition)}",
                    $"En rekvisition har registrerats för tolkuppdrag med boknings-ID {order.OrderNumber}. {GoToOrderButton(order.OrderId, HtmlHelper.ViewTab.Requisition)}",
                    notificationType
                );
            }
        }

        public async Task RequisitionReviewed(Requisition requisition)
        {
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(RequisitionReviewed), nameof(NotificationService));
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $@"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har granskats.

Sammanställning:

{await GetRequisitionPriceInformationForMail(requisition)}";
            var email = GetOrganisationNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionReviewed, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har granskats",
                    body + GoToRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    NotificationType.RequisitionReviewed,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionReviewed, NotificationChannel.Webhook);
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
            var body = $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(requisition.Request)} har kommenterats av myndighet. Följande kommentar har angivits:\n{requisition.CustomerComment}";
            var email = GetOrganisationNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionCommented, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation,
                    $"Rekvisition för tolkuppdrag med boknings-ID {orderNumber} har kommenterats",
                    body + GoToRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    HtmlHelper.ToHtmlBreak(body) + GoToRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                    NotificationType.RequisitionCommented,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(requisition.Request.Ranking.BrokerId, NotificationType.RequisitionCommented, NotificationChannel.Webhook);
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
            var body = $"Myndigheten {customer.Name} har lagts till i {Constants.SystemName}.\nMyndighetens organisationsnummer: {customer.OrganisationNumber}\nMyndighetens Peppol-ID: {customer.PeppolId}\n\n Vid användning av tjänstens API kan myndigheten identifieras med denna identifierare: {customer.OrganisationPrefix}";
            foreach (int brokerId in _dbContext.Brokers.Select(b => b.BrokerId).ToList())
            {
                var email = GetOrganisationNotificationSettings(brokerId, NotificationType.CustomerAdded, NotificationChannel.Email);
                if (email != null)
                {
                    CreateEmail(email.ContactInformation, $"En ny myndighet har lagts upp i systemet.", body, null, NotificationType.CustomerAdded);
                }
                var webhook = GetOrganisationNotificationSettings(brokerId, NotificationType.CustomerAdded, NotificationChannel.Webhook);
                if (webhook != null)
                {
                    CreateWebHookCall(new CustomerCreatedModel
                    {
                        Key = customer.OrganisationPrefix,
                        OrganisationNumber = customer.OrganisationNumber,
                        PeppolId = customer.PeppolId,
                        PriceListType = customer.PriceListType.GetCustomName(),
                        Name = customer.Name,
                        Description = customer.ParentCustomerOrganisationId != null ? $"Organiserad under {customer.ParentCustomerOrganisation.Name}" : null,
                        TravelCostAgreementType = customer.TravelCostAgreementType.GetCustomName(),
                        UseSelfInvoicingInterpreter = customer.CustomerSettings.SingleOrDefault(cs => cs.CustomerSettingType == CustomerSettingType.UseSelfInvoicingInterpreter).Value
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
            NotificationType notificationType = NotificationType.OrderAnswered;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                var body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats. {GetRequireApprovementText(request.LatestAnswerTimeForCustomer)}\n\n" +
                    OrderReferenceNumberInfo(request.Order) +
                    $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                    $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                    InterpreterCompetenceInfo(request.CompetenceLevel) +
                    GetPossibleInfoNotValidatedInterpreter(request);

                CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har accepterat bokningsförfrågan {orderNumber}",
                    body + GoToOrderPlain(request.Order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                    notificationType
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Order.CustomerOrganisationId, NotificationType.OrderAnswered, NotificationChannel.Webhook, NotificationConsumerType.Customer);
            if (webhook != null)
            {
                CreateWebHookCall(new OrderAnsweredModel
                {
                    OrderNumber = orderNumber,
                    BrokerKey = request.Ranking.Broker.OrganizationPrefix
                },
                webhook.ContactInformation,
                webhook.NotificationType,
                webhook.RecipientUserId);
            }
        }

        public void RequestGroupAccepted(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupAccepted), nameof(NotificationService));
            NotificationType notificationType = NotificationType.OrderGroupAccepted;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
                Order order = requestGroup.OrderGroup.FirstOrder;
                var body = $"Svar på sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Bokningsförfrågan har accepterats. {GetRequireApprovementText(requestGroup.LatestAnswerTimeForCustomer)}\n\n" +
                    OrderReferenceNumberInfo(order) +
                    $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tTillfällen: \n" +
                    $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                    GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
                if (requestGroup.HasExtraInterpreter)
                {
                    body += GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForExtraInterpreter, true);
                }
                CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                    $"Förmedling har accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                    body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId),
                    notificationType
                );
            }
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
            NotificationType notificationType = NotificationType.OrderDeclined;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                var body = $"Svar på bokningsförfrågan {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till bokningsförfrågan med följande meddelande:\n{request.DenyMessage} \n\nBokningsförfrågan skickas nu automatiskt vidare till nästa förmedling enligt rangordningen förutsatt att det finns ytterligare förmedlingar att fråga. I de fall en bokningsförfrågan avslutas på grund av att ingen förmedling har kunnat tillsätta en tolk så skickas ett e-postmeddelande till er om detta.";
                CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har tackat nej till bokningsförfrågan {orderNumber}",
                    body + GoToOrderPlain(request.Order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                    notificationType
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Order.CustomerOrganisationId, NotificationType.OrderDeclined, NotificationChannel.Webhook, NotificationConsumerType.Customer);
            if (webhook != null)
            {
                CreateWebHookCall(new OrderDeclinedModel
                {
                    OrderNumber = orderNumber,
                    Message = request.DenyMessage,
                    BrokerKey = request.Ranking.Broker.OrganizationPrefix
                },
                webhook.ContactInformation,
                webhook.NotificationType,
                webhook.RecipientUserId);
            }
        }

        public void RequestGroupDeclinedByBroker(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RequestGroupDeclinedByBroker), nameof(NotificationService));
            NotificationType notificationType = NotificationType.OrderGroupDeclined;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
                var body = $"Svar på sammanhållna bokningsförfrågan {orderGroupNumber} har inkommit. Förmedling {requestGroup.Ranking.Broker.Name} har tackat nej till den sammanhållna bokningsförfrågan med följande meddelande:\n{requestGroup.DenyMessage} \n\nBokningsförfrågan skickas nu automatiskt vidare till nästa förmedling enligt rangordningen förutsatt att det finns ytterligare förmedlingar att fråga. I de fall en bokningsförfrågan avslutas på grund av att ingen förmedling har kunnat tillsätta en tolk så skickas ett e-postmeddelande till er om detta.";
                CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup), $"Förmedling har tackat nej till den sammanhållna bokningsförfrågan {orderGroupNumber}",
                    body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId),
                    notificationType
                );
            }
        }

        public void RequestCompleted(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCompleted), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            var body = $"Tiden för tolkuppdrag med boknings-ID {orderNumber} {RequestReferenceNumberInfo(request)} har passerat. Det är nu möjligt att registrera en rekvisition för uppdraget eller arkivera bokningen som avslutad utan att göra en rekvisition. För att komma till bokningen följ länken nedan:";
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAssignmentTimePassed, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(
                    email.ContactInformation,
                    $"Tiden för tillsatt tolkuppdrag {orderNumber} har passerat",
                    body + GoToRequestPlain(request.RequestId),
                    body + GoToRequestButton(request.RequestId),
                    NotificationType.RequestAssignmentTimePassed,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAssignmentTimePassed, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                new RequestCompletedModel
                {
                    OrderNumber = orderNumber,
                },
                webhook.ContactInformation,
                NotificationType.RequestAssignmentTimePassed,
                webhook.RecipientUserId);
            }
        }

        public void RequestCancelledByBroker(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestCancelledByBroker), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            NotificationType notificationType = NotificationType.OrderCancelledByBroker;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                var body = $"Förmedling {request.Ranking.Broker.Name} har avbokat tolkuppdraget med boknings-ID {orderNumber} med meddelande:\n{request.CancelMessage}";
                CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har avbokat tolkuppdraget med boknings-ID {orderNumber}",
                    body + GoToOrderPlain(request.Order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                    notificationType
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Order.CustomerOrganisationId, NotificationType.OrderCancelledByBroker, NotificationChannel.Webhook, NotificationConsumerType.Customer);
            if (webhook != null)
            {
                CreateWebHookCall(new OrderCancelledModel
                {
                    OrderNumber = orderNumber,
                    Message = request.DenyMessage,
                    BrokerKey = request.Ranking.Broker.OrganizationPrefix
                },
                webhook.ContactInformation,
                webhook.NotificationType,
                webhook.RecipientUserId);
            }
        }

        public void RequestReplamentOrderAccepted(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestReplamentOrderAccepted), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            NotificationType notificationType;

            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    notificationType = NotificationType.ReplamentOrderAccepted;
                    if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
                    {
                        var body = $"Svar på ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Ersättningsuppdrag har accepterats. {GetRequireApprovementText(request.LatestAnswerTimeForCustomer)}";
                        CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                            body + GoToOrderPlain(request.Order.OrderId),
                            HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                            notificationType
                        );
                    }
                    break;
                case RequestStatus.Approved:
                    notificationType = NotificationType.ReplamentOrderApproved;
                    if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
                    {
                        var bodyAppr = $"Ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har accepteras. Inga förändrade krav finns, tolkuppdrag är klart för utförande.";
                        CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                            bodyAppr + GoToOrderPlain(request.Order.OrderId, HtmlHelper.ViewTab.Default, true),
                            HtmlHelper.ToHtmlBreak(bodyAppr) + GoToOrderButton(request.Order.OrderId, HtmlHelper.ViewTab.Default, null, true, true),
                            notificationType
                        );
                    }
                    NotifyCustomerOnAcceptedAnswer(request, orderNumber);
                    NotifyBrokerOnAcceptedAnswer(request, orderNumber);
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestReplamentOrderAccepted)} cannot send notifications on requests with status: {request.Status}");
            }
        }

        public void RequestReplamentOrderDeclinedByBroker(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestReplamentOrderDeclinedByBroker), nameof(NotificationService));
            NotificationType notificationType = NotificationType.ReplamentOrderDeclined;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderNumber = request.Order.OrderNumber;

                var body = $"Svar på ersättningsuppdrag {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} " +
                    $"har tackat nej till ersättningsuppdrag med följande meddelande:\n{request.DenyMessage}";

                CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har tackat nej till ersättningsuppdrag {orderNumber}",
                    $"{body} {GoToOrderPlain(request.Order.OrderId)}",
                    $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderButton(request.Order.OrderId)}",
                    notificationType
                );
            }
        }

        public void RequestChangedInterpreter(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestChangedInterpreter), nameof(NotificationService));
            NotificationType notificationType = NotificationType.InterpreterChanged;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderNumber = request.Order.OrderNumber;

                var body = $"Nytt svar på bokningsförfrågan med boknings-ID {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk för uppdraget.\n\n" +
                    OrderReferenceNumberInfo(request.Order) +
                    $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                    $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                    $"{InterpreterCompetenceInfo(request.CompetenceLevel)}\n\n" + GetRequireApprovementText(request.LatestAnswerTimeForCustomer)
                    + GetPossibleInfoNotValidatedInterpreter(request);
                CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har bytt tolk för uppdrag med boknings-ID {orderNumber}",
                    $"{body} {GoToOrderPlain(request.Order.OrderId)}",
                    $"{HtmlHelper.ToHtmlBreak(body)} {GoToOrderButton(request.Order.OrderId)}",
                    notificationType
                );
            }
        }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RequestChangedInterpreterAccepted), nameof(NotificationService));
            string orderNumber = request.Order.OrderNumber;
            //Broker
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestReplacedInterpreterAccepted, NotificationChannel.Email);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Byte av tolk godkänt för boknings-ID {orderNumber}",
                $"Bytet av tolk har godkänts för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(request)}. {GoToRequestPlain(request.RequestId)}",
                $"Bytet av tolk har godkänts för tolkuppdrag med boknings-ID {orderNumber}{RequestReferenceNumberInfo(request)}. {GoToRequestButton(request.RequestId)}",
                NotificationType.RequestReplacedInterpreterAccepted,
                true
            );
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestReplacedInterpreterAccepted, NotificationChannel.Webhook);
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
                    NotificationType notificationType = NotificationType.RequestReplacedInterpreterAccepted;
                    if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
                    {
                        var bodyNoAccept = $"Nytt svar på bokningsförfrågan {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk för uppdraget.\n\n" +
                        OrderReferenceNumberInfo(request.Order) +
                        $"Språk: {request.Order.OtherLanguage ?? request.Order.Language?.Name}\n" +
                        $"Datum och tid för uppdrag: {request.Order.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{request.Order.EndAt.ToSwedishString("HH:mm")}\n" +
                        $"{InterpreterCompetenceInfo(request.CompetenceLevel)}\n\n" +
                        "Tolkbytet är godkänt av systemet." +
                        GetPossibleInfoNotValidatedInterpreter(request);
                        CreateEmail(GetRecipientsFromOrder(request.Order), $"Förmedling har bytt tolk för uppdrag med boknings-ID {orderNumber}",
                            $"{bodyNoAccept} {GoToOrderPlain(request.Order.OrderId, HtmlHelper.ViewTab.Default, true)}",
                            $"{HtmlHelper.ToHtmlBreak(bodyNoAccept)} {GoToOrderButton(request.Order.OrderId, HtmlHelper.ViewTab.Default, null, true, true)}",
                            notificationType
                        );
                    }
                    break;
                case InterpereterChangeAcceptOrigin.User:
                    //No mail to customer if it was the customer that accepted.
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestChangedInterpreterAccepted)} failed to send mail to customer. {changeOrigin} is not a handled {nameof(InterpereterChangeAcceptOrigin)}");
            }
        }

        public void RemindUnhandledRequest(Request request)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(RemindUnhandledRequest), nameof(NotificationService));
            NotificationType notificationType = NotificationType.RemindUnhandledRequest;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderNumber = request.Order.OrderNumber;
                string body = $"Svar på bokningsförfrågan {orderNumber} från förmedling {request.Ranking.Broker.Name} väntar på hantering. Bokningsförfrågan har "
                + (request.Status == RequestStatus.AcceptedNewInterpreterAppointed ? "ändrats med ny tolk. " : "accepterats. ")
                + GetRequireApprovementText(request.LatestAnswerTimeForCustomer);

                CreateEmail(GetRecipientsFromOrder(request.Order), $"Bokningsförfrågan {orderNumber} väntar på hantering",
                    body + GoToOrderPlain(request.Order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderButton(request.Order.OrderId),
                    notificationType
                );
            }
        }

        public void RemindUnhandledRequestGroup(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(RemindUnhandledRequestGroup), nameof(NotificationService));
            NotificationType notificationType = NotificationType.RemindUnhandledRequestGroup;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderNumber = requestGroup.OrderGroup.OrderGroupNumber;
                string body = $"Svar på sammanhållen bokningsförfrågan {orderNumber} från förmedling {requestGroup.Ranking.Broker.Name} väntar på hantering. Bokningsförfrågan har accepterats."
                + GetRequireApprovementText(requestGroup.LatestAnswerTimeForCustomer);

                CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup), $"Sammanhållen bokningsförfrågan {orderNumber} väntar på hantering",
                    body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId),
                    notificationType
                );
            }
        }

        public void PartialRequestGroupAnswerAccepted(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(PartialRequestGroupAnswerAccepted), nameof(NotificationService));
            NotificationType notificationType = NotificationType.PartialRequestGroupAccepted;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
                Order order = requestGroup.OrderGroup.FirstOrder;
                var body = $"Svar på sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Del av bokningsförfrågan har accepterats.\n" +
                    $"Den extra tolk som avropades har gått vidare som en egen förfrågan till nästa förmedling. {GetRequireApprovementText(requestGroup.LatestAnswerTimeForCustomer)}\n\n" +
                    $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tTillfällen: \n" +
                    $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                    GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
                CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                    $"Förmedling har delvis accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                    body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId),
                    notificationType
                );
            }
        }

        public void PartialRequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup)
        {
            NullCheckHelper.ArgumentCheckNull(requestGroup, nameof(PartialRequestGroupAnswerAutomaticallyApproved), nameof(NotificationService));
            string orderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber;
            NotificationType notificationType = NotificationType.PartialRequestGroupAutomaticallyApproved;
            if (NotficationTypeAvailable(notificationType, NotificationConsumerType.Customer, NotificationChannel.Email) && !NotficationTypeExcludedForCustomer(notificationType))
            {
                Order order = requestGroup.OrderGroup.FirstOrder;

                var body = $"Svar på sammanhållen bokningsförfrågan {orderGroupNumber} från förmedling {requestGroup.Ranking.Broker.Name} har inkommit. Del av bokningsförfrågan har accepterats.\n\n" +
                    $"Språk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tTillfällen: \n" +
                    $"{GetOccurences(requestGroup.OrderGroup.Orders)}\n" +
                    GetPossibleInfoNotValidatedInterpreter(requestGroup.FirstRequestForFirstInterpreter);
                CreateEmail(GetRecipientsFromOrderGroup(requestGroup.OrderGroup),
                    $"Förmedling har delvis accepterat sammanhållen bokningsförfrågan {orderGroupNumber}",
                    body + GoToOrderGroupPlain(requestGroup.OrderGroup.OrderGroupId),
                    HtmlHelper.ToHtmlBreak(body) + GoToOrderGroupButton(requestGroup.OrderGroup.OrderGroupId),
                    notificationType
                );
            }
            NotifyBrokerOnAcceptedAnswer(requestGroup, orderGroupNumber);
        }

        public void CreateEmail(string recipient, string subject, string plainBody, string htmlBody, NotificationType notificationType, bool isBrokerMail = false, bool addContractInfo = true)
        {
            CreateEmail(new[] { recipient }, subject, plainBody, string.IsNullOrEmpty(htmlBody) ? HtmlHelper.ToHtmlBreak(plainBody) : htmlBody, notificationType, isBrokerMail, addContractInfo);
        }

        public void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, NotificationType notificationType, int replacingEmailId, int resentByUserId)
        {
            _dbContext.Add(new OutboundEmail(
                    recipient,
                    subject,
                    plainBody,
                    htmlBody,
                    _clock.SwedenNow,
                    notificationType,
                    replacingEmailId,
                    resentByUserId));
            _dbContext.SaveChanges();
        }

        public async Task NotifyOnFailedWebHook(int callId)
        {
            OutboundWebHookCall call = await _dbContext.OutboundWebHookCalls.GetOutboundWebHookCall(callId);
            var recipientId = call.RecipientUser.BrokerId ?? call.RecipientUser.CustomerOrganisationId ?? call.RecipientUserId;
            var consumerType = call.RecipientUser.CustomerOrganisationId.HasValue ? NotificationConsumerType.Customer : NotificationConsumerType.Broker;
            var email = GetOrganisationNotificationSettings(recipientId, NotificationType.ErrorNotification, NotificationChannel.Email, consumerType);
            if (email != null)
            {
                CreateEmail(email.ContactInformation, $"Ett webhook-anrop från {Constants.SystemName} har misslyckats fem gånger",
                $@"Webhook misslyckades av typ: {call.NotificationType.GetDescription()}({call.NotificationType.GetCustomName()})
{GoToWebHookListPlain()}",
                $@"Webhook misslyckades av typ: {call.NotificationType.GetDescription()}({call.NotificationType.GetCustomName()})<br/>
{GoToWebHookListButton("Gå till systemets loggsida")}",
                NotificationType.ErrorNotification,
                true
            );
            }
            var webhook = GetOrganisationNotificationSettings(recipientId, NotificationType.ErrorNotification, NotificationChannel.Webhook, consumerType);
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
                    $@"Webhook misslyckades av typ: {call.NotificationType.GetDescription()}({call.NotificationType.GetCustomName()}) till {(consumerType == NotificationConsumerType.Customer ? "CustomerOrganisationId" : "BrokerId")}:{recipientId}",
                    null,
                    NotificationType.ErrorNotification
                );
            }
        }

        public async Task NotifyOnFailedPeppolMessage(int messageId)
        {
            OutboundPeppolMessage message = await _dbContext.OutboundPeppolMessages.GetOutboundPeppolMessage(messageId);
            var recipientId = message.OrderAgreementPayload.Request.Order.CustomerOrganisationId;
            if (_tolkBaseOptions.Support.ReportPeppolMessageFailures)
            {
                CreateEmail(
                    _tolkBaseOptions.Support.SecondLineEmail,
                    "Ett Peppolmeddelande har misslyckats fem gånger",
                    $@"Peppolmeddelande misslyckades av typ: {message.NotificationType.GetDescription()}({message.NotificationType.GetCustomName()}) till CustomerOrganisationId: {recipientId}",
                    null,
                    NotificationType.ErrorNotification
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

            var webhook = GetOrganisationNotificationSettings((int)brokerId, failedCall.NotificationType, NotificationChannel.Webhook);

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

        public bool ResendPeppolMessage(OutboundPeppolMessage failedMessage, int? resentUserId = null, int? resentImpersonatorUserId = null)
        {
            NullCheckHelper.ArgumentCheckNull(failedMessage, nameof(ResendPeppolMessage), nameof(NotificationService));

            OutboundPeppolMessage newMessage = new OutboundPeppolMessage(
                Guid.NewGuid().ToString(),
                failedMessage.Recipient,
                failedMessage.Payload,
                _clock.SwedenNow,
                failedMessage.NotificationType,
                resentUserId,
                resentImpersonatorUserId,
                failedMessage.OutboundPeppolMessageId);

            _dbContext.OutboundPeppolMessages.Add(newMessage);
            _dbContext.SaveChanges();

            return true;
        }

        private string GetRequestGroupExpiredDueToNoAnswerFromCustomerText(OrderGroup ordergroup, RequestGroup requestGroup) => !requestGroup.LatestAnswerTimeForCustomer.HasValue ?
            $"{ordergroup.CustomerOrganisation.Name} med organisationsnummer {ordergroup.CustomerOrganisation.OrganisationNumber}{RequestReferenceNumberInfo(requestGroup)} har inte besvarat tillsättningen på den sammanhållna bokningsförfrågan {ordergroup.OrderGroupNumber} innan det första uppdraget startade, därför har nu den sammanhållna bokningsförfrågan avslutats." :
            $"{ordergroup.CustomerOrganisation.Name} med organisationsnummer {ordergroup.CustomerOrganisation.OrganisationNumber}{RequestReferenceNumberInfo(requestGroup)} har inte besvarat tillsättningen på den sammanhållna bokningsförfrågan {ordergroup.OrderGroupNumber} inom den tid er förmedling har angivit som sista svarstid. Den sammanhållna bokningsförfrågan har nu avslutats.";

        private string GetOrderChangeText(Order order, OrderChangeLogEntry lastEntry, string interpreterLocationText)
        {
            StringBuilder sb = new StringBuilder("Följande fält har ändrats på bokningen:\n");

            foreach (OrderHistoryEntry oh in lastEntry.OrderHistories)
            {
                switch (oh.ChangeOrderType)
                {
                    case ChangeOrderType.LocationStreet:
                    case ChangeOrderType.OffSiteContactInformation:
                        sb.Append(GetOrderFieldText(interpreterLocationText, oh));
                        break;
                    case ChangeOrderType.Description:
                        sb.Append(GetOrderFieldText(order.Description, oh));
                        break;
                    case ChangeOrderType.InvoiceReference:
                        sb.Append(GetOrderFieldText(order.InvoiceReference, oh));
                        break;
                    case ChangeOrderType.CustomerReferenceNumber:
                        sb.Append(GetOrderFieldText(order.CustomerReferenceNumber, oh));
                        break;
                    case ChangeOrderType.CustomerDepartment:
                        sb.Append(GetOrderFieldText(order.UnitName, oh));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return sb.ToString();
        }

        private static string GetOrderFieldText(string newValue, OrderHistoryEntry oh)
        {
            return (string.IsNullOrEmpty(newValue) && string.IsNullOrEmpty(oh.Value)) ? string.Empty :
                string.IsNullOrEmpty(newValue) ? $"{oh.ChangeOrderType.GetDescription()} - Fältet är nu tomt\n" :
                newValue.Equals(oh.Value, StringComparison.OrdinalIgnoreCase) ? string.Empty :
                $"{oh.ChangeOrderType.GetDescription()} - Nytt värde: {newValue}\n";
        }

        private static string GetOrderTerminatedText(OrderStatus status, string orderNumber) => status == OrderStatus.NoDeadlineFromCustomer ? $"Ingen sista svarstid sattes på bokningsförfrågan {orderNumber} så att förfrågan kunde gå vidare till nästa förmedling. Bokningsförfrågan är nu avslutad."
            : status == OrderStatus.ResponseNotAnsweredByCreator ? $"Tolktillsättning för bokningsförfrågan {orderNumber} besvarades inte i tid. Bokningsförfrågan är nu avslutad." :
            $"Ingen förmedling kunde tillsätta en tolk för bokningsförfrågan {orderNumber}. Bokningsförfrågan är nu avslutad.";

        private static string GetOrderGroupTerminatedText(OrderStatus status, string orderNumber) => status == OrderStatus.NoDeadlineFromCustomer ?
            $"Ingen sista svarstid sattes på den sammanhållna bokningsförfrågan {orderNumber} så att den kunde gå vidare till nästa förmedling. Den sammanhållna bokningsförfrågan är nu avslutad." :
            status == OrderStatus.ResponseNotAnsweredByCreator ? $"Tolktillsättning för sammanhållna bokningsförfrågan {orderNumber} besvarades inte i tid. Den sammanhållna bokningsförfrågan är nu avslutad." :
            $"Ingen förmedling kunde tillsätta en tolk för den sammanhållna bokningsförfrågan {orderNumber}. Den sammanhållna bokningsförfrågan är nu avslutad.";

        private string GetOccurences(IEnumerable<Order> orders)
        {
            var texts = orders.Select(o => $"\t\t{o.OrderNumber}: {o.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{o.EndAt.ToSwedishString("HH:mm") + (o.IsExtraInterpreterForOrderId.HasValue ? " Extra tolk" : "")}").ToArray();
            return string.Join("\n", texts);
        }

        private string GetOccurencesAsHtmlList(IEnumerable<Order> orders)
        {
            var texts = orders.Select(o => $"<li>{o.OrderNumber}: {o.StartAt.ToSwedishString("yyyy-MM-dd HH:mm")}-{o.EndAt.ToSwedishString("HH:mm") + (o.IsExtraInterpreterForOrderId.HasValue ? " Extra tolk" : "")}").ToArray();
            return $"<ul>{string.Join("\n", texts)}</ul>";
        }

        private string GetRequestExpiredDueToNoAnswerFromCustomerText(Order order, Request request) => !request.LatestAnswerTimeForCustomer.HasValue ?
                    $"{order.CustomerOrganisation.Name} med organisationsnummer {order.CustomerOrganisation.OrganisationNumber} har inte besvarat tillsättningen på bokningsförfrågan {order.OrderNumber}{RequestReferenceNumberInfo(request)} innan uppdraget startade, därför har nu bokningsförfrågan avslutats." :
                    $"{order.CustomerOrganisation.Name} med organisationsnummer {order.CustomerOrganisation.OrganisationNumber} har inte besvarat tillsättningen på bokningsförfrågan {order.OrderNumber}{RequestReferenceNumberInfo(request)} inom den tid er förmedling har angivit som sista svarstid. Bokningsförfrågan har nu avslutats.";

        private string OrderReferenceNumberInfo(Order order)
        {
            return string.IsNullOrWhiteSpace(order.CustomerReferenceNumber) ? string.Empty : $"Myndighetens ärendenummer: {order.CustomerReferenceNumber}\n";
        }

        private string RequestReferenceNumberInfo(RequestBase request)
        {
            return string.IsNullOrWhiteSpace(request.BrokerReferenceNumber) ? string.Empty : $" (ert bokningsnummer: {request.BrokerReferenceNumber})";
        }

        private string InterpreterCompetenceInfo(int? competenceInfo)
        {
            return $"Tolkens kompetensnivå: {((CompetenceAndSpecialistLevel?)competenceInfo)?.GetDescription() ?? "Information saknas"}";
        }

        private string GetPossibleInfoNotValidatedInterpreter(Request request, bool isExtraInterpreter = false)
        {
            var interpreter = isExtraInterpreter ? "tillsatt extra tolk" : "tillsatt tolk";
            var shouldCheckValidationCode = _tolkBaseOptions.Tellus.IsActivated && request.InterpreterCompetenceVerificationResultOnAssign.HasValue;
            bool isInterpreterValidationError = shouldCheckValidationCode && (request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.UnknownError || request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.ConnectionError);
            bool isInterpreterVerified = request.InterpreterCompetenceVerificationResultOnAssign == VerificationResult.Validated;
            return isInterpreterValidationError ? $"\n\nObservera att {interpreter}s kompetensnivå inte har gått att kontrollera mot Kammarkollegiets tolkregister pga att det inte gick att nå tolkregistret. Risk finns att ställda krav på kompetensnivå inte uppfylls. Mer information finns i Kammarkollegiets tolkregister." : (shouldCheckValidationCode && !isInterpreterVerified) ? $"\n\nObservera att {interpreter} för tolkuppdraget inte finns registrerad i Kammarkollegiets tolkregister med kravställd/önskad kompetensnivå för detta språk. Risk finns att ställda krav på kompetensnivå inte uppfylls. Mer information finns i Kammarkollegiets tolkregister." : string.Empty;
        }

        private static string GetRequireApprovementText(DateTimeOffset? latestAnswerDate) => latestAnswerDate.HasValue ?
            $"Observera att ni måste godkänna tillsatt tolk för tolkuppdraget eftersom ni har begärt att få förhandsgodkänna resekostnader. Senaste svarstid för att godkänna tillsättning är {latestAnswerDate.Value.ToSwedishString("yyyy-MM-dd HH:mm")}. Om tillsättning inte besvarats vid denna tidpunkt kommer bokningen att annulleras." :
            "Observera att ni måste godkänna tillsatt tolk för tolkuppdraget eftersom ni har begärt att få förhandsgodkänna resekostnader. Om godkännande inte görs kommer bokningen att annulleras.";

        private void CreateEmail(IEnumerable<string> recipients, string subject, string plainBody, string htmlBody, NotificationType notificationType, bool isBrokerMail = false, bool addContractInfo = true)
        {
            string noReply = "Detta e-postmeddelande går inte att svara på.";
            string handledBy = $"Detta ärende hanteras i {Constants.SystemName}.";
            string contractInfo = $"Avrop från ramavtal för tolkförmedlingstjänster {Constants.ContractNumber}";

            foreach (string recipient in recipients)
            {
                _dbContext.Add(new OutboundEmail(
                    recipient,
                    _senderPrepend + subject,
                    $"{plainBody}\n\n{noReply}" + (isBrokerMail ? $"\n\n{handledBy}" : "") + (addContractInfo ? $"\n\n{contractInfo}" : ""),
                    $"{htmlBody}<br/><br/>{noReply}" + (isBrokerMail ? $"<br/><br/>{handledBy}" : "") + (addContractInfo ? $"<br/><br/>{contractInfo}" : ""),
                    _clock.SwedenNow,
                    notificationType
                ));
            }
            _dbContext.SaveChanges();
        }

        private async Task<string> GetRequisitionPriceInformationForMail(Requisition requisition)
        {
            var prices = await _dbContext.RequisitionPriceRows.GetPriceRowsForRequisition(requisition.RequisitionId).ToListAsync();
            DisplayPriceInformation priceInfo = PriceCalculationService.GetPriceInformationToDisplay(prices.OfType<PriceRowBase>());
            string invoiceInfo = string.Empty;
            invoiceInfo += $"Följande tolktaxa har använts för beräkning: {priceInfo.PriceListTypeDescription} {priceInfo.CompetencePriceDescription}\n\n";
            foreach (DisplayPriceRow dpr in priceInfo.DisplayPriceRows)
            {
                invoiceInfo += $"{dpr.Description}:\n{dpr.Price.ToSwedishString("#,0.00 SEK")}\n\n";
            }
            invoiceInfo += $"Total summa: {priceInfo.TotalPrice.ToSwedishString("#,0.00 SEK")}";
            return invoiceInfo;
        }

        private void NotifyBrokerOnAcceptedAnswer(Request request, string orderNumber)
        {
            var email = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerApproved, NotificationChannel.Email);
            if (email != null)
            {
                var body = $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av tolk på bokningsförfrågan {orderNumber}{RequestReferenceNumberInfo(request)}.";
                CreateEmail(email.ContactInformation, $"Tolkuppdrag med boknings-ID {orderNumber} verifierat",
                    body + GoToRequestPlain(request.RequestId),
                    body + GoToRequestButton(request.RequestId),
                    NotificationType.RequestAnswerApproved,
                    true
                );
            }
            var webhook = GetOrganisationNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerApproved, NotificationChannel.Webhook);
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
        private void NotifyCustomerOnAcceptedAnswer(Request request, string orderNumber)
        {
            var webhook = GetOrganisationNotificationSettings(request.Order.CustomerOrganisationId, NotificationType.OrderAccepted, NotificationChannel.Webhook, NotificationConsumerType.Customer);
            if (webhook != null)
            {
                CreateWebHookCall(new OrderAcceptedModel
                {
                    OrderNumber = orderNumber,
                    BrokerKey = request.Ranking.Broker.OrganizationPrefix
                },
                webhook.ContactInformation,
                webhook.NotificationType,
                webhook.RecipientUserId);
            }
        }

        private void NotifyBrokerOnAcceptedAnswer(RequestGroup requestGroup, string orderGroupNumber)
        {
            var email = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerApproved, NotificationChannel.Email);
            if (email != null)
            {
                var body = $"{requestGroup.OrderGroup.CustomerOrganisation.Name} har godkänt tillsättningen av tolk på den sammanhållna bokningsförfrågan {orderGroupNumber}{RequestReferenceNumberInfo(requestGroup)}.";
                CreateEmail(email.ContactInformation, $"Sammanhållen bokning med boknings-ID {orderGroupNumber} verifierat",
                        body + GoToRequestGroupPlain(requestGroup.RequestGroupId),
                        body + GoToRequestGroupButton(requestGroup.RequestGroupId),
                        NotificationType.RequestGroupAnswerApproved,
                        true);
            }
            var webhook = GetOrganisationNotificationSettings(requestGroup.Ranking.BrokerId, NotificationType.RequestGroupAnswerApproved, NotificationChannel.Webhook);
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

        private OrganisationNotificationSettings GetOrganisationNotificationSettings(int consumerId, NotificationType type, NotificationChannel channel, NotificationConsumerType consumerType = NotificationConsumerType.Broker)
        {
            if (!NotficationTypeAvailable(type, consumerType, channel))
            {
                return null;
            }
            if (!_cacheService.OrganisationNotificationSettings.Any(b => b.ReceivingOrganisationId == consumerId && b.NotificationConsumerType == consumerType) && channel == NotificationChannel.Email)
            {
                return new OrganisationNotificationSettings
                {
                    ContactInformation = _dbContext.Brokers.Single(b => b.BrokerId == consumerId).EmailAddress,
                };
            }
            return _cacheService.OrganisationNotificationSettings.SingleOrDefault(b => b.ReceivingOrganisationId == consumerId && b.NotificationConsumerType == consumerType && b.NotificationType == type && b.NotificationChannel == channel);
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
            return tab switch
            {
                HtmlHelper.ViewTab.Requisition => $"\n\n\nGå till rekvisition: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=requisition")}",
                HtmlHelper.ViewTab.Complaint => $"\n\n\nGå till reklamation: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=complaint")}",
                _ => $"\n\n\nGå till bokningsförfrågan: {HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId)}",
            };
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
            return tab switch
            {
                HtmlHelper.ViewTab.Requisition => breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=requisition"), "Till rekvisition"),
                HtmlHelper.ViewTab.Complaint => breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId, "tab=complaint"), "Till reklamation"),
                _ => breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestId), "Till bokning"),
            };
        }

        private string GoToRequestGroupButton(int requestGroupId, string textOverride = null, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestGroupViewUrl(_tolkBaseOptions.TolkWebBaseUrl, requestGroupId), textOverride ?? "Till bokning");
        }

        private async Task<RequestModel> GetRequestModel(Request request)
        {
            var order = await _dbContext.Orders.GetFullOrderById(request.OrderId);
            var priceRows = _priceCalculationService.GetPrices(request, (CompetenceAndSpecialistLevel)order.PriceCalculatedFromCompetenceLevel, null).PriceRows.ToList();
            var calculationCharges = _dbContext.PriceCalculationCharges.GetPriceCalculationChargesByIds(priceRows.Where(p => p.PriceCalculationChargeId.HasValue).Select(p => p.PriceCalculationChargeId.Value).ToList());
            priceRows.Where(p => p.PriceCalculationChargeId.HasValue).ToList().ForEach(p => p.PriceCalculationCharge = new PriceCalculationCharge { ChargePercentage = calculationCharges.Where(c => c.PriceCalculationChargeId == p.PriceCalculationChargeId).FirstOrDefault().ChargePercentage });

            return new RequestModel
            {
                CreatedAt = request.CreatedAt,
                OrderNumber = order.OrderNumber,
                BrokerReferenceNumber = request.BrokerReferenceNumber,
                CustomerInformation = new CustomerInformationModel
                {
                    Name = order.CustomerOrganisation.Name,
                    Key = order.CustomerOrganisation.OrganisationPrefix,
                    OrganisationNumber = order.CustomerOrganisation.OrganisationNumber,
                    PeppolId = order.CustomerOrganisation.PeppolId,
                    ContactName = order.CreatedByUser.FullName,
                    ContactPhone = order.ContactPhone,
                    ContactEmail = order.ContactEmail,
                    InvoiceReference = order.InvoiceReference,
                    PriceListType = order.CustomerOrganisation.PriceListType.GetCustomName(),
                    TravelCostAgreementType = order.CustomerOrganisation.TravelCostAgreementType.GetCustomName(),
                    ReferenceNumber = order.CustomerReferenceNumber,
                    UnitName = order.CustomerUnit?.Name,
                    DepartmentName = order.UnitName,
                    UseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter))
                },
                //D2 pads any single digit with a zero 1 -> "01"
                Region = order.RegionId.ToSwedishString("D2"),
                Language = new LanguageModel
                {
                    Key = order.Language?.ISO_639_Code,
                    Description = order.OtherLanguage ?? order.Language.Name,
                },
                ExpiresAt = request.ExpiresAt,
                StartAt = order.StartAt,
                EndAt = order.EndAt,
                Locations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(order.OrderId).Select(l => new LocationModel
                {
                    OffsiteContactInformation = l.OffSiteContactInformation,
                    Street = l.Street,
                    City = l.City,
                    Rank = l.Rank,
                    Key = EnumHelper.GetCustomName(l.InterpreterLocation)
                }).ToListAsync(),
                CompetenceLevels = await _dbContext.OrderCompetenceRequirements.GetOrderedCompetenceRequirementsForOrder(order.OrderId).Select(c => new CompetenceModel
                {
                    Key = EnumHelper.GetCustomName(c.CompetenceLevel),
                    Rank = c.Rank ?? 0
                }).ToListAsync(),
                AllowMoreThanTwoHoursTravelTime = order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                CreatorIsInterpreterUser = order.CreatorIsInterpreterUser,
                AssignmentType = EnumHelper.GetCustomName(order.AssignmentType),
                Description = order.Description,
                CompetenceLevelsAreRequired = order.SpecificCompetenceLevelRequired,
                MealBreakIncluded = order.MealBreakIncluded,
                Requirements = await _dbContext.OrderRequirements.GetRequirementsForOrder(order.OrderId).Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderRequirementId,
                    RequirementType = EnumHelper.GetCustomName(r.RequirementType)
                }).ToListAsync(),
                Attachments = await _dbContext.Attachments.GetAttachmentsForOrderAndGroup(order.OrderId, order.OrderGroupId).Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName
                }).ToListAsync(),
                PriceInformation = priceRows.GetPriceInformationModel(order.PriceCalculatedFromCompetenceLevel.GetCustomName(), request.Ranking.BrokerFee)
            };
        }

        private RequestUpdatedModel GetRequestUpdatedModel(Order order, bool attachmentUpdated, bool orderFieldsUpdated, OrderChangeLogEntry lastChange, InterpreterLocation interpreterLocationFromAnswer, string interpreterLocationText)
        {
            RequestUpdatedModel updatedModel = new RequestUpdatedModel { OrderNumber = order.OrderNumber };
            updatedModel.RequestUpdateType = (orderFieldsUpdated && attachmentUpdated) ? OrderChangeLogType.AttachmentAndOrderInformationFields.GetCustomName() : attachmentUpdated ? OrderChangeLogType.Attachment.GetCustomName() : OrderChangeLogType.OrderInformationFields.GetCustomName();

            var attachments = attachmentUpdated ? _dbContext.Attachments.GetAttachmentsForOrderAndGroup(order.OrderId, order.OrderGroupId).Select(a => new AttachmentInformationModel
            {
                AttachmentId = a.AttachmentId,
                FileName = a.FileName
            }) : null;

            if (orderFieldsUpdated)
            {
                bool interpreterLocationUpdated = false;
                bool descriptionUpdated = false;
                bool invoiceReferenceUpdated = false;
                bool customerReferenceNumberUpdated = false;
                bool customerDepartmentNumberUpdated = false;

                foreach (OrderHistoryEntry oh in lastChange.OrderHistories)
                {
                    switch (oh.ChangeOrderType)
                    {
                        case ChangeOrderType.LocationStreet:
                        case ChangeOrderType.OffSiteContactInformation:
                            interpreterLocationUpdated = !string.IsNullOrEmpty(GetOrderFieldText(interpreterLocationText, oh));
                            break;
                        case ChangeOrderType.Description:
                            descriptionUpdated = !string.IsNullOrEmpty(GetOrderFieldText(order.Description, oh));
                            break;
                        case ChangeOrderType.InvoiceReference:
                            invoiceReferenceUpdated = !string.IsNullOrEmpty(GetOrderFieldText(order.InvoiceReference, oh));
                            break;
                        case ChangeOrderType.CustomerReferenceNumber:
                            customerReferenceNumberUpdated = !string.IsNullOrEmpty(GetOrderFieldText(order.CustomerReferenceNumber, oh));
                            break;
                        case ChangeOrderType.CustomerDepartment:
                            customerDepartmentNumberUpdated = !string.IsNullOrEmpty(GetOrderFieldText(order.UnitName, oh));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                if (customerReferenceNumberUpdated || customerDepartmentNumberUpdated || invoiceReferenceUpdated)
                {
                    updatedModel.CustomerInformationUpdated = new CustomerInformationUpdatedModel();
                    if (customerReferenceNumberUpdated)
                    {
                        updatedModel.CustomerInformationUpdated.ReferenceNumber = order.CustomerReferenceNumber ?? string.Empty;
                    }
                    if (invoiceReferenceUpdated)
                    {
                        updatedModel.CustomerInformationUpdated.InvoiceReference = order.InvoiceReference;
                    }
                    if (customerDepartmentNumberUpdated)
                    {
                        updatedModel.CustomerInformationUpdated.DepartmentName = order.UnitName ?? string.Empty;
                    }
                }
                if (interpreterLocationUpdated)
                {
                    updatedModel.LocationUpdated = new LocationUpdatedModel();
                    if (interpreterLocationFromAnswer == InterpreterLocation.OffSitePhone || interpreterLocationFromAnswer == InterpreterLocation.OffSiteVideo)
                    {
                        updatedModel.LocationUpdated.OffsiteContactInformation = interpreterLocationText;
                    }
                    else
                    {
                        updatedModel.LocationUpdated.Street = interpreterLocationText;
                    }
                }
                if (descriptionUpdated)
                {
                    updatedModel.Description = order.Description ?? string.Empty;
                }
            }
            if (attachmentUpdated)
            {
                updatedModel.Attachments = attachments;
            }
            return updatedModel;
        }

        private string GoToWebHookListPlain() => $"\n\n\nGå till systemets loggsida för att få mer information : {HtmlHelper.GetWebHookListUrl(_tolkBaseOptions.TolkWebBaseUrl)}";

        private string GoToWebHookListButton(string textOverride, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetWebHookListUrl(_tolkBaseOptions.TolkWebBaseUrl), textOverride);
        }

        private async Task<RequestGroupModel> GetRequestGroupModel(RequestGroup requestGroup)
        {
            var orderGroup = requestGroup.OrderGroup;
            var order = await _dbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroupId).OrderBy(o => o.OrderId).FirstAsync();
            var locations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(order.OrderId).ToListAsync();
            var competenceRequirements = await _dbContext.OrderGroupCompetenceRequirements.GetOrderedCompetenceRequirementsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            var requirements = await _dbContext.OrderGroupRequirements.GetRequirementsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            var attachments = await _dbContext.Attachments.GetAttachmentsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            var priceRows = await _dbContext.OrderPriceRows.GetPriceRowsForOrdersInOrderGroup(requestGroup.OrderGroupId).ToListAsync();
            return new RequestGroupModel
            {
                CreatedAt = requestGroup.CreatedAt,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                CustomerInformation = new CustomerInformationModel
                {
                    Name = orderGroup.CustomerOrganisation.Name,
                    Key = orderGroup.CustomerOrganisation.OrganisationPrefix,
                    OrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                    PeppolId = orderGroup.CustomerOrganisation.PeppolId,
                    ContactName = orderGroup.CreatedByUser.FullName,
                    ContactPhone = orderGroup.ContactPhone,
                    ContactEmail = orderGroup.ContactEmail,
                    InvoiceReference = order.InvoiceReference,
                    PriceListType = orderGroup.CustomerOrganisation.PriceListType.GetCustomName(),
                    TravelCostAgreementType = orderGroup.CustomerOrganisation.TravelCostAgreementType.GetCustomName(),
                    ReferenceNumber = order.CustomerReferenceNumber,
                    UnitName = orderGroup.CustomerUnit?.Name,
                    DepartmentName = order.UnitName,
                    UseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == orderGroup.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter))
                },
                //D2 pads any single digit with a zero 1 -> "01"
                Region = orderGroup.Region.RegionId.ToSwedishString("D2"),
                Language = new LanguageModel
                {
                    Key = orderGroup.Language?.ISO_639_Code,
                    Description = orderGroup.OtherLanguage ?? orderGroup.Language.Name,
                },
                ExpiresAt = requestGroup.ExpiresAt,
                Locations = locations.Select(l => new LocationModel
                {
                    OffsiteContactInformation = l.OffSiteContactInformation,
                    Street = l.Street,
                    City = l.City,
                    Rank = l.Rank,
                    Key = EnumHelper.GetCustomName(l.InterpreterLocation)
                }),
                CompetenceLevels = competenceRequirements.Select(c => new CompetenceModel
                {
                    Key = EnumHelper.GetCustomName(c.CompetenceLevel),
                    Rank = c.Rank ?? 0
                }),
                AllowMoreThanTwoHoursTravelTime = orderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                CreatorIsInterpreterUser = orderGroup.CreatorIsInterpreterUser,
                AssignmentType = EnumHelper.GetCustomName(orderGroup.AssignmentType),
                Description = order.Description,
                CompetenceLevelsAreRequired = orderGroup.SpecificCompetenceLevelRequired,
                Requirements = requirements.Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderGroupRequirementId,
                    RequirementType = EnumHelper.GetCustomName(r.RequirementType)
                }),
                Attachments = attachments.Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName
                }),
                Occasions = orderGroup.Orders.Select(o => new OccasionModel
                {
                    OrderNumber = o.OrderNumber,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    IsExtraInterpreterForOrderNumber = o.IsExtraInterpreterForOrder?.OrderNumber,
                    PriceInformation = priceRows.Where(p => p.OrderId == o.OrderId).GetPriceInformationModel(o.PriceCalculatedFromCompetenceLevel.GetCustomName(), requestGroup.Ranking.BrokerFee),
                    MealBreakIncluded = o.MealBreakIncluded
                })
            };
        }

        private  bool NotficationTypeExcludedForCustomer(NotificationType nt) => !string.IsNullOrWhiteSpace(_tolkBaseOptions.ExcludedNotificationTypesForCustomer) && _tolkBaseOptions.ExcludedNotificationTypesForCustomer.Split(",").Any(e => int.Parse(e) == (int)nt);

        private static bool NotficationTypeAvailable(NotificationType nt, NotificationConsumerType consumer, NotificationChannel channel) => nt.GetAvailableNotificationChannels().Any(ch => ch == channel) && nt.GetAvailableNotificationConsumerTypes().Any(c => c == consumer);

        private async Task<Request> GetRequest(int id)
        {
            var request = await _dbContext.Requests.GetRequestForWebHook(id);
            request.Order.Attachments = await _dbContext.OrderAttachments.GetAttachmentsForOrder(request.OrderId).ToListAsync();
            request.Order.Requirements = await _dbContext.OrderRequirements.GetRequirementsForOrder(request.OrderId).ToListAsync();
            request.Order.PriceRows = await _dbContext.OrderPriceRows.GetPriceRowsForOrder(request.OrderId).ToListAsync();
            request.Order.InterpreterLocations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.OrderId).ToListAsync();
            return request;
        }
    }
}
