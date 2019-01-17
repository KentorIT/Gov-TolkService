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
    public class NotificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISwedishClock _clock;
        private readonly TolkOptions _options;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IMemoryCache _cache;
        private const string brokerSettingsCacheKey = nameof(brokerSettingsCacheKey);

        private static readonly HttpClient client = new HttpClient();

        public static readonly string NoReplyText = "Detta e-postmeddelande går inte att svara på.";

        public static readonly string NoReplyTextPlain = "\n\n" + NoReplyText;

        public static readonly string NoReplyTextHtml = HtmlHelper.ToHtmlBreak(NoReplyTextPlain);

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock,
            IOptions<TolkOptions> options,
            PriceCalculationService priceCalculationService,
            IMemoryCache cache
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _options = options.Value;
            _priceCalculationService = priceCalculationService;
            _cache = cache;
        }

        public void OrderCancelledByCustomer(Request request, bool requestWasApproved, bool createFullCompensationRequisition)
        {
            string orderNumber = request.Order.OrderNumber;
            string broker = request.Ranking.Broker.EmailAddress;
            if (requestWasApproved)
            {
                string body = $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                     $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                     (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.") +
                     NoReplyTextPlain;
                CreateEmail(broker, $"Avbokat avrop avrops-ID {orderNumber}",
                    body + GotoRequestPlain(request.RequestId),
                    HtmlHelper.ToHtmlBreak(body) + GotoRequestButton(request.RequestId));
            }
            else
            {
                var body = $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                    $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                    NoReplyTextPlain;
                CreateEmail(broker, $"Avbokad förfrågan avrops-ID {orderNumber}",
                    body + GotoRequestPlain(request.RequestId),
                    HtmlHelper.ToHtmlBreak(body) + GotoRequestButton(request.RequestId));
            }
        }

        public void OrderContactPersonChanged(Order order)
        {
            AspNetUser previousContactUser = order.OrderContactPersonHistory.OrderByDescending(cph => cph.OrderContactPersonHistoryId).First().PreviousContactPersonUser;
            AspNetUser currentContactUser = order.ContactPersonUser;

            string orderNumber = order.OrderNumber;

            string subject = $"Behörighet ändrad för tolkuppdrag avrops-ID {orderNumber}";

            if (!string.IsNullOrEmpty(previousContactUser?.Email))
            {
                string body = $"Behörighet att godkänna eller underkänna rekvisition har ändrats. Du har inte längre denna behörighet för avrop {orderNumber}." + NoReplyTextPlain;
                CreateEmail(previousContactUser.Email, subject,
                    body + GotoOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(order.OrderId));
            }
            if (!string.IsNullOrEmpty(currentContactUser?.Email))
            {
                string body = $"Behörighet att godkänna eller underkänna rekvisition har ändrats. Du har nu behörighet att utföra detta för avrop {orderNumber}." + NoReplyTextPlain;
                CreateEmail(currentContactUser.Email, subject,
                    body + GotoOrderPlain(order.OrderId),
                    HtmlHelper.ToHtmlBreak(body) + GotoOrderButton(order.OrderId));
            }
            //Broker
            var request = order.Requests.Single(r =>
                            r.Status == RequestStatus.Created ||
                            r.Status == RequestStatus.Received ||
                            r.Status == RequestStatus.Accepted ||
                            r.Status == RequestStatus.Approved ||
                            r.Status == RequestStatus.AcceptedNewInterpreterAppointed);
            string bodyBroker = "Kontaktperson har ändrats på avrop {orderNumber}." + NoReplyTextPlain;
            CreateEmail(request.Ranking.Broker.EmailAddress, $"Avrop {order.OrderNumber} har uppdaterats",
                bodyBroker + GotoRequestPlain(request.RequestId),
                HtmlHelper.ToHtmlBreak(bodyBroker) + GotoRequestButton(request.RequestId));
        }

        public void OrderReplacementCreated(Order order)
        {
            Request oldRequest = order.Requests.SingleOrDefault(r => r.Status == RequestStatus.CancelledByCreator);

            Order replacementOrder = order.ReplacedByOrder;
            Request replacementRequest = replacementOrder.Requests.Single();
            var bodyPlain = $"\tOrginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tOrginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tErsättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tErsättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tTolk: {replacementRequest.Interpreter.FullName}, e-post: {replacementRequest.Interpreter.Email}\n" +
                $"\tSvara senast: {replacementRequest.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}\n\n\n" +
                $"Gå till ersättningsuppdrag: {HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, replacementRequest.RequestId)}\n" +
                $"Gå till ursprungligt uppdrag: {HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, oldRequest.RequestId)}";
            var bodyHtml = $@"
<ul>
<li>Orginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Orginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Ersättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Tolk: {replacementRequest.Interpreter.FullName}, e-post: {replacementRequest.Interpreter.Email}</li>
<li>Svara senast: {replacementRequest.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{NoReplyText}</div>
<div>{GotoRequestButton(replacementRequest.RequestId, textOverride: "Gå till ersättningsuppdrag", autoBreakLines: false)}</div>
<div>{GotoRequestButton(oldRequest.RequestId, textOverride: "Gå till ursprungligt uppdrag", autoBreakLines: false)}</div>";
            CreateEmail(
                replacementRequest.Ranking.Broker.EmailAddress,
                $"Avrop {order.OrderNumber} har avbokats, med ersättningsuppdrag: {replacementOrder.OrderNumber}",
                bodyPlain,
                bodyHtml);
        }

        public void OrderNoBrokerAccepted(Order order)
        {
            CreateEmail(GetRecipiantsFromOrder(order),
                $"Avrop fick ingen tolk: {order.OrderNumber}",
                $"Ingen förmedling kunde tillsätta en tolk för avrop {order.OrderNumber}. {NoReplyTextPlain} {GotoOrderPlain(order.OrderId)}",
                $"Ingen förmedling kunde tillsätta en tolk för avrop {order.OrderNumber}. {NoReplyTextHtml} {GotoOrderButton(order.OrderId)}"
            );
        }

        public void RequestCreated(Request request)
        {
            var order = request.Order;
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Email);
            if (email != null)
            {
                string bodyPlain = $"En ny bokningsförfrågan har kommit in från {order.CustomerOrganisation.Name}.\n" +
                    $"\tRegion: {order.Region.Name}\n" +
                    $"\tSpråk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tStart: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSlut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSvara senast: {request.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}\n\n\n" +
                    NoReplyTextPlain +
                    GotoRequestPlain(request.RequestId);
                string bodyHtml = $@"En ny bokningsförfrågan har kommit in från {order.CustomerOrganisation.Name}.<br />
<ul>
<li>Region: {order.Region.Name}</li>
<li>Språk: {order.OtherLanguage ?? order.Language?.Name}</li>
<li>Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}</li>
<li>Svara senast: {request.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}</li>
</ul>
<div>{NoReplyText}</div>
<div>{GotoRequestButton(request.RequestId, textOverride: "Till förfrågan", autoBreakLines: false)}</div>";
                CreateEmail(
                    email.ContactInformation,
                    $"Nytt avrop registrerat: {order.OrderNumber}",
                    bodyPlain,
                    bodyHtml
                );
            }
            var webhook = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated, NotificationChannel.Webhook);
            if (webhook != null)
            {
                CreateWebHookCall(
                    new RequestModel
                    {
                        CreatedAt = request.CreatedAt,
                        OrderNumber = order.OrderNumber,
                        Customer = order.CustomerOrganisation.Name,
                        //D2 pads any single digit with a zero 1 -> "01"
                        Region = order.Region.RegionId.ToString("D2"),
                        Language = new LanguageModel
                        {
                            Key = request.Order.Language?.ISO_639_Code,
                            Description = request.Order.OtherLanguage ?? request.Order.Language.Name,
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
                        AllowMoreThanTwoHoursTravelTime = order.AllowMoreThanTwoHoursTravelTime,
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
                    },
                    webhook.ContactInformation,
                    NotificationType.RequestCreated,
                    webhook.RecipientUserId
                );
            }
        }

        public void RequestAnswerAutomaticallyAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat avrop {orderNumber}",
                $"Svar på avrop {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Avropet har accepterats." +
                $"\n\nTolk:\n{request.Interpreter.CompleteContactInformation}");

            NotifyBrokerOnAcceptedAnswer(request, orderNumber);
        }

        public void RequestAnswerApproved(Request request)
        {
            NotifyBrokerOnAcceptedAnswer(request, request.Order.OrderNumber);
        }

        public void RequestAnswerDenied(Request request)
        {
            CreateEmail(request.Ranking.Broker.EmailAddress,
                $"Svar på avropsförfrågan med  avrops-ID {request.Order.OrderNumber} har underkänts",
                $"Ert svar på avrop {request.Order.OrderNumber} underkändes med följande meddelande:\n{request.DenyMessage}. {NoReplyTextPlain} {GotoRequestPlain(request.RequestId)}",
                $"Ert svar på avrop {request.Order.OrderNumber} underkändes med följande meddelande:<br />{request.DenyMessage}. {NoReplyTextHtml} {GotoRequestButton(request.RequestId)}"
            );
        }

        public void ComplaintCreated(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.Request.Ranking.Broker.EmailAddress, $"En reklamation har registrerats på avrop {orderNumber}",
                $@"Reklamation för avrop {orderNumber} har skapats med följande meddelande:
{complaint.ComplaintType.GetDescription()}
{complaint.ComplaintMessage} 
{NoReplyTextPlain}
{GotoRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                $@"Reklamation för avrop {orderNumber} har skapats med följande meddelande:<br />
{complaint.ComplaintType.GetDescription()}<br />
{complaint.ComplaintMessage}
{NoReplyTextHtml}
{GotoRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintDisputed(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.CreatedByUser.Email, $"Reklamation kopplad till tolkuppdrag {orderNumber} har blivit bestriden",
                $"Reklamation för avrop {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage} {NoReplyTextPlain} {GotoOrderPlain(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}",
                $"Reklamation för avrop {orderNumber} har bestridits med följande meddelande:<br />{complaint.AnswerMessage} {NoReplyTextHtml} {GotoOrderButton(complaint.Request.Order.OrderId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintDisputePendingTrial(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.Request.Ranking.Broker.EmailAddress, $"Ert bestridande av reklamation avslogs på avrop {orderNumber}",
                $"Bestridande av reklamation för avrop {orderNumber} har avslagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {NoReplyTextPlain} {GotoRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                $"Bestridande av reklamation för avrop {orderNumber} har avslagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {NoReplyTextHtml} {GotoRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void ComplaintTerminatedAsDisputeAccepted(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.Request.Ranking.Broker.EmailAddress, $"Ert bestridande av reklamation har godtagits på avrop {orderNumber}",
                $"Bestridande av reklamation för avrop {orderNumber} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage} {NoReplyTextPlain} {GotoRequestPlain(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}",
                $"Bestridande av reklamation för avrop {orderNumber} har godtagits med följande meddelande:<br />{complaint.AnswerDisputedMessage} {NoReplyTextHtml} {GotoRequestButton(complaint.Request.RequestId, HtmlHelper.ViewTab.Complaint)}"
            );
        }

        public void RequisitionCreated(Requisition requisition)
        {
            var order = requisition.Request.Order;
            CreateEmail(GetRecipiantsFromOrder(order),
                $"En rekvisition har registrerats på avrop {order.OrderNumber}",
                $"En rekvisition har registrerats på avrop {order.OrderNumber}. {NoReplyTextPlain} {GotoOrderPlain(order.OrderId, HtmlHelper.ViewTab.Requisition)}",
                $"En rekvisition har registrerats på avrop {order.OrderNumber}. {NoReplyTextHtml} {GotoOrderButton(order.OrderId, HtmlHelper.ViewTab.Requisition)}"
            );
        }

        public void RequisitionApproved(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $@"Rekvisition för avrop {orderNumber} har godkänts

Kostnader att fakturera:

{GetRequisitionPriceInformationForMail(requisition)}";
            var bodyPlain = body + NoReplyTextPlain;
            var bodyHtml = HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml;
            //Customer
            CreateEmail(requisition.CreatedByUser.Email,
                $"Rekvisition för avrop {orderNumber} har godkänts",
                bodyPlain + GotoOrderPlain(requisition.Request.Order.OrderId, HtmlHelper.ViewTab.Requisition),
                bodyHtml + GotoOrderButton(requisition.Request.Order.OrderId, HtmlHelper.ViewTab.Requisition)
            );
            //Broker
            CreateEmail(requisition.Request.Ranking.Broker.EmailAddress,
                $"Rekvisition för avrop {orderNumber} har godkänts",
                bodyPlain + GotoRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                bodyHtml + GotoRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition)
            );
        }

        public void RequisitionDenied(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            var body = $"Rekvisition för avrop {orderNumber} har underkänts med följande meddelande:\n{requisition.DenyMessage}";
            var bodyPlain = body + NoReplyTextPlain;
            var bodyHtml = HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml;
            List<string> receipents = new List<string>() { requisition.CreatedByUser.Email };
            //Customer
            CreateEmail(requisition.CreatedByUser.Email,
                $"Rekvisition för avrop {orderNumber} har underkänts",
                bodyPlain + GotoOrderPlain(requisition.Request.Order.OrderId, HtmlHelper.ViewTab.Requisition),
                bodyHtml + GotoOrderButton(requisition.Request.Order.OrderId, HtmlHelper.ViewTab.Requisition)
            );
            //Broker
            CreateEmail(requisition.Request.Ranking.Broker.EmailAddress,
                $"Rekvisition för avrop {orderNumber} har underkänts",
                bodyPlain + GotoRequestPlain(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition),
                bodyHtml + GotoRequestButton(requisition.Request.RequestId, HtmlHelper.ViewTab.Requisition)
            );
        }

        public void RequestAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $@"Svar på avrop {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Avropet har accepterats.
Du behöver godkänna de beräknade resekostnaderna.

Tolk:
{request.Interpreter.CompleteContactInformation}";
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat avrop {orderNumber}",
                body + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
        }

        public void RequestDeclinedByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $"Svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till avropet med följande meddelande:\n{request.DenyMessage}";
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har tackat nej till avrop {orderNumber}",
                body + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
        }

        public void RequestCancelledByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $"Förmedling {request.Ranking.Broker.Name} har avbokat uppdraget för avrop {orderNumber} med meddelande:\n{request.CancelMessage}";
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har avbokat avrop {orderNumber}",
                body + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
        }

        public void RequestReplamentOrderAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    var body = $"Svar på ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Ersättningsuppdrag har accepterats. Eventuellt förändrade svar finns som måste beaktas.";
                    CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        body + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                        HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
                    break;
                case RequestStatus.Approved:
                    var bodyAppr = $"Ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har accepteras. Inga förändrade krav finns, avropet är klart för utförande.";
                    CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        bodyAppr + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                        HtmlHelper.ToHtmlBreak(bodyAppr) + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
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
                $"{body} {NoReplyTextPlain} {GotoOrderPlain(request.Order.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {NoReplyTextHtml} {GotoOrderButton(request.Order.OrderId)}");
        }

        public void RequestChangedInterpreter(Request request)
        {
            string orderNumber = request.Order.OrderNumber;

            var body = $"Nytt svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk på avropet.\n" +
                $"\nTolk:\n{request.Interpreter.CompleteContactInformation}\n\n" +
                (request.Order.AllowMoreThanTwoHoursTravelTime ?
                    "Eventuellt förändrade krav finns som måste beaktas. Om byte av tolk på avropet inte godkänns/avslås så kommer systemet godkänna avropet automatiskt " +
                    $"{_options.HoursToApproveChangeInterpreterRequests} timmar före uppdraget startar förutsatt att avropet tidigare haft status godkänt." :
                    "Inga förändrade krav finns, avropet behåller sin nuvarande status.");
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har bytt tolk på avrop {orderNumber}",
                $"{body} {NoReplyTextPlain} {GotoOrderPlain(request.Order.OrderId)}",
                $"{HtmlHelper.ToHtmlBreak(body)} {NoReplyTextHtml} {GotoOrderButton(request.Order.OrderId)}");
        }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User)
        {
            string orderNumber = request.Order.OrderNumber;
            //Broker
            CreateEmail(request.Ranking.Broker.EmailAddress, $"Byte av tolk godkänt på Avrops-id {orderNumber}",
                $"Bytet av tolk har godkänts på order Avrops-id {orderNumber}. {NoReplyTextPlain} {GotoRequestPlain(request.RequestId)}",
                $"Bytet av tolk har godkänts på order Avrops-id {orderNumber}. {NoReplyTextHtml} {GotoRequestButton(request.RequestId)}"
            );
            //Creator
            switch (changeOrigin)
            {
                case InterpereterChangeAcceptOrigin.SystemRule:
                    var body = $"Svar på avrop {request.Order.OrderNumber} där tolk har bytts ut har godkänts av systemet då uppdraget " +
                        $"startar inom {_options.HoursToApproveChangeInterpreterRequests} timmar. " +
                        $"Uppdraget startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.";
                    CreateEmail(request.Order.CreatedByUser.Email,
                        $"Svar på avrop med avrops-ID {request.Order.OrderNumber} har godkänts av systemet",
                        $"{body} {NoReplyTextPlain} {GotoOrderPlain(request.Order.OrderId)}",
                        $"{HtmlHelper.ToHtmlBreak(body)} {NoReplyTextHtml} {GotoOrderButton(request.Order.OrderId)}"
                    );
                    break;
                case InterpereterChangeAcceptOrigin.NoNeedForUserAccept:
                    var bodyNoAccept = $"Nytt svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk på avropet.\n" +
                        $"\nTolk:\n{request.Interpreter.CompleteContactInformation}\n\n" +
                        "Inga förändrade krav finns, avropet behåller sin nuvarande status.";
                    CreateEmail(request.Order.CreatedByUser.Email, $"Förmedling har bytt tolk på avrop {orderNumber}",
                        $"{bodyNoAccept} {NoReplyTextPlain} {GotoOrderPlain(request.Order.OrderId)}",
                        $"{HtmlHelper.ToHtmlBreak(bodyNoAccept)} {NoReplyTextHtml} {GotoOrderButton(request.Order.OrderId)}"
                    );
                    break;
                case InterpereterChangeAcceptOrigin.User:
                    //No mail to customer if it was the customer that accepted.
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestChangedInterpreterAccepted)} faild to send mail to customer. {changeOrigin.ToString()} is not a handled {nameof(InterpereterChangeAcceptOrigin)}");
            }
        }

        public void RemindUnhandledRequest(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            var body = $@"Svar på avrop {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Avropet har accepterats.
Du behöver godkänna de beräknade resekostnaderna.

Tolk:
{request.Interpreter.CompleteContactInformation}";
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Avrop {orderNumber} väntar på hantering",
                body + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                HtmlHelper.ToHtmlBreak(body) + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
        }

        public void CreateEmail(string recipient, string subject, string plainBody)
        {
            CreateEmail(new[] { recipient }, subject, plainBody, HtmlHelper.ToHtmlBreak(plainBody));
        }

        public void CreateEmail(string recipient, string subject, string plainBody, string htmlBody)
        {
            CreateEmail(new[] { recipient }, subject, plainBody, htmlBody);
        }

        private void CreateEmail(IEnumerable<string> recipients, string subject, string plainBody)
        {
            CreateEmail(recipients, subject, plainBody, HtmlHelper.ToHtmlBreak(plainBody));
        }

        private void CreateEmail(IEnumerable<string> recipients, string subject, string plainBody, string htmlBody)
        {
            foreach (string recipient in recipients)
            {
                _dbContext.Add(new OutboundEmail(
                    recipient,
                    subject,
                    plainBody,
                    htmlBody,
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
                invoiceInfo += $"Summa totalt att fakturera: {priceInfo.TotalPrice.ToString("#,0.00 SEK")}";
                return invoiceInfo;
            }
        }

        private void NotifyBrokerOnAcceptedAnswer(Request request, string orderNumber)
        {
            //Broker part
            var email = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestAnswerApproved, NotificationChannel.Email);
            if (email != null)
            {
                var body = $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av {request.Interpreter.FullName} på avrop {orderNumber}.";
                CreateEmail(request.Ranking.Broker.EmailAddress, $"Tolkuppdrag med avrops-ID {orderNumber} verifierat",
                        body + NoReplyTextPlain + GotoOrderPlain(request.Order.OrderId),
                        body + NoReplyTextHtml + GotoOrderButton(request.Order.OrderId));
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

        private static IEnumerable<string> GetRecipiantsFromOrder(Order order)
        {
            yield return order.CreatedByUser.Email;
            if (order.ContactPersonId.HasValue)
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

        public IEnumerable<BrokerNotificationSettings> BrokerNotificationSettings
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
                    return $"\n\n\nGå till avrop: {HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId)}";
                case HtmlHelper.ViewTab.Requisition:
                    return $"\n\n\nGå till rekvisition: {HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId)}?tab=requisition";
                case HtmlHelper.ViewTab.Complaint:
                    return $"\n\n\nGå till reklamation: {HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId)}?tab=complaint";
            }
        }

        private string GotoRequestPlain(int requestId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default)
        {
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return $"\n\n\nGå till avrop: {HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId)}";
                case HtmlHelper.ViewTab.Requisition:
                    return $"\n\n\nGå till rekvisition: {HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId)}?tab=requisition";
                case HtmlHelper.ViewTab.Complaint:
                    return $"\n\n\nGå till reklamation: {HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId)}?tab=complaint";
            }
        }

        private string GotoOrderButton(int orderId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, string textOverride = null, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            if (!string.IsNullOrEmpty(textOverride))
            {
                return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId), textOverride);
            }
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId), "Till bokning");
                case HtmlHelper.ViewTab.Requisition:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId)}?tab=requisition", "Till rekvisition");
                case HtmlHelper.ViewTab.Complaint:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetOrderViewUrl(_options.PublicOrigin, orderId)}?tab=complaint", "Till reklamation");
            }
        }

        private string GotoRequestButton(int requestId, HtmlHelper.ViewTab tab = HtmlHelper.ViewTab.Default, string textOverride = null, bool autoBreakLines = true)
        {
            string breakLines = autoBreakLines ? "<br /><br /><br />" : "";
            if (!string.IsNullOrEmpty(textOverride))
            {
                return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId), textOverride);
            }
            switch (tab)
            {
                case HtmlHelper.ViewTab.Default:
                default:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag(HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId), "Till bokning");
                case HtmlHelper.ViewTab.Requisition:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId)}?tab=requisition", "Till rekvisition");
                case HtmlHelper.ViewTab.Complaint:
                    return breakLines + HtmlHelper.GetButtonDefaultLargeTag($"{HtmlHelper.GetRequestViewUrl(_options.PublicOrigin, requestId)}?tab=complaint", "Till reklamation");
            }
        }
    }
}
