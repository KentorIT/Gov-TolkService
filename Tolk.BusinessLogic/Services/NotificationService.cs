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

        private static readonly HttpClient client = new HttpClient();

        public NotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock,
            IOptions<TolkOptions> options,
            PriceCalculationService priceCalculationService
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _options = options.Value;
            _priceCalculationService = priceCalculationService;
        }

        public void OrderCancelledByCustomer(Request request, bool requestWasApproved, bool createFullCompensationRequisition)
        {
            string orderNumber = request.Order.OrderNumber;
            string broker = request.Ranking.Broker.EmailAddress;
            if (requestWasApproved)
            {
                CreateEmail(
                    request.Interpreter.User.Email,
                    $"Avbokat avrop avrops-ID {orderNumber}",
                    $"Ditt tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                    $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                    (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.")
                );
                CreateEmail(broker, $"Avbokat avrop avrops-ID {orderNumber}",
                     $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                     $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                     (createFullCompensationRequisition ? "\nDetta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "\nDetta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.")
                );
            }
            else
            {
                CreateEmail(broker, $"Avbokad förfrågan avrops-ID {request.Order.OrderNumber}",
                    $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n{request.CancelMessage}\n" +
                    $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}."
                );
            }
        }

        public void OrderContactPersonChanged(Order order)
        {
            AspNetUser previousContactUser = order.OrderContactPersonHistory.OrderByDescending(cph => cph.OrderContactPersonHistoryId).First().PreviousContactPersonUser;
            AspNetUser currentContactUser = order.ContactPersonUser;

            string orderNumber = order.OrderNumber;

            string subject = $"Behörighet ändrad för tolkuppdrag avrops-ID {orderNumber}";
            string bodyPreviousContact = $"Behörighet att godkänna eller underkänna rekvisition har ändrats. Du har inte längre denna behörighet för avrop {orderNumber}.";
            string bodyCurrentContact = $"Behörighet att godkänna eller underkänna rekvisition har ändrats. Du har nu behörighet att utföra detta för avrop {orderNumber}.";

            if (!string.IsNullOrEmpty(previousContactUser?.Email))
            {
                CreateEmail(previousContactUser.Email, subject, bodyPreviousContact);
            }
            if (!string.IsNullOrEmpty(currentContactUser?.Email))
            {
                CreateEmail(currentContactUser.Email, subject, bodyCurrentContact);
            }
            //Broker
            var request = order.Requests.Single(r =>
                            r.Status == RequestStatus.Created ||
                            r.Status == RequestStatus.Received ||
                            r.Status == RequestStatus.Accepted ||
                            r.Status == RequestStatus.Approved ||
                            r.Status == RequestStatus.AcceptedNewInterpreterAppointed);
            CreateEmail(request.Ranking.Broker.EmailAddress, $"Avrop {order.OrderNumber} har uppdaterats", "Kontaktperson har ändrats.");
        }

        public void OrderReplacementCreated(Order order)
        {
            Order replacementOrder = order.ReplacedByOrder;
            Request replamentRequest = replacementOrder.Requests.Single();
            CreateEmail(
                replamentRequest.Ranking.Broker.EmailAddress,
                $"Avrop {order.OrderNumber} har avbokats, med ersättningsuppdrag: {replacementOrder.OrderNumber}",
                $"\tOrginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tOrginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tErsättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tErsättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                $"\tTolk: {replamentRequest.Interpreter.User.FullName}, e-post: {replamentRequest.Interpreter.User.Email}\n" +
                $"\tSvara senast: {replamentRequest.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}\n\n"
            );
        }

        public void OrderNoBrokerAccepted(Order order)
        {
            CreateEmail(GetRecipiantsFromOrder(order),
                $"Avrop fick ingen tolk: {order.OrderNumber}",
                $"Ingen förmedling kunde tillsätta en tolk för detta tillfälle."
            );
        }

        public void RequestCreated(Request request)
        {
            //This makes including Broker object unecessary
            var settings = GetBrokerNotificationSettings(request.Ranking.BrokerId, NotificationType.RequestCreated);

            var order = request.Order;
            if (settings.SendEmail)
            {
                if (string.IsNullOrWhiteSpace(settings.EmailAddress))
                {
                    //Temporary fix...
                    settings.EmailAddress = request.Ranking.Broker.EmailAddress;
                }
                    CreateEmail(
                    settings.EmailAddress,
                    $"Nytt avrop registrerat: {order.OrderNumber}",
                    $"Ett nytt avrop har kommit in från {order.CustomerOrganisation.Name}.\n" +
                    $"\tRegion: {order.Region.Name}\n" +
                $"\tSpråk: {order.OtherLanguage ?? order.Language?.Name}\n" +
                    $"\tStart: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSlut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                    $"\tSvara senast: {request.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}");
            }
            if (settings.CallWebhook)
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
                            Key = request.Order.Language?.ISO_639_1_Code,
                            Description = request.Order.OtherLanguage ?? request.Order.Language.Name,
                            Dialect = request.Order.Requirements.SingleOrDefault(r => r.RequirementType == RequirementType.Dialect)?.Description,
                            DialectIsRequired = request.Order.Requirements.SingleOrDefault(r => r.RequirementType == RequirementType.Dialect)?.IsRequired ?? false
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
                        HasFiles = order.Attachments.Any(),
                        //Need to aggregate the price list types
                        PriceInformation = new PriceInformationModel
                        {
                            PriceCalculatedFromCompetenceLevel = order.PriceCalculatedFromCompetenceLevel.GetCustomName(),
                            PriceRows = order.PriceRows.GroupBy(r => r.PriceRowType)
                            .Select(p => new PriceRowModel
                            {
                                Description = p.Key.GetDescription(),
                                PriceRowType = p.Key.GetCustomName(),
                                Price = p.Count() == 1 ? p.Sum(s => s.TotalPrice ) : 0,
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
                    settings.Webhook,
                    NotificationType.RequestCreated,
                    settings.RecipientUserId
                );
            }
        }

        public void RequestAnswerAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            //Interpreter part
            NotifyInterpreterOnAssignenment(request);

            //Broker part
            CreateEmail(request.Ranking.Broker.EmailAddress, $"Tolkuppdrag med avrops-ID {orderNumber} verifierat",
                $"{request.Order.CustomerOrganisation.Name} har godkänt tillsättningen av {request.Interpreter.User.FullName}."
            );
        }

        public void RequestAnswerDenied(Request request)
        {
            CreateEmail(request.Ranking.Broker.EmailAddress,
                $"Svar på avropsförfrågan med  avrops-ID {request.Order.OrderNumber} har underkänts",
                $"Svaret underkändes med följande meddelande:\n{request.DenyMessage} ."
            );
        }

        public void ComplaintCreated(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.Request.Ranking.Broker.EmailAddress, $"En reklamation har registrerats på avrop {orderNumber}",
                $"Reklamation för avrop {orderNumber} har skapats med följande meddelande:\n{complaint.ComplaintType.GetDescription()}\n" +
                $"{complaint.ComplaintMessage}"
            );
        }

        public void ComplaintDisputed(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.CreatedByUser.Email, $"Reklamation kopplad till tolkuppdrag {orderNumber} har blivit bestriden",
                $"Reklamation för avrop {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage}"
            );
        }

        public void ComplaintDisputePendingTrial(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.Request.Ranking.Broker.EmailAddress, $"Ert bestridande av reklamation avslogs på avrop {orderNumber}",
                $"Bestridande av reklamation för avrop {orderNumber} har avslagits med följande meddelande:\n{complaint.AnswerDisputedMessage}"
            );
        }

        public void ComplaintTerminatedAsDisputeAccepted(Complaint complaint)
        {
            string orderNumber = complaint.Request.Order.OrderNumber;
            CreateEmail(complaint.Request.Ranking.Broker.EmailAddress, $"Ert bestridande av reklamation har godtagits på avrop {orderNumber}",
                $"Bestridande av reklamation för avrop {orderNumber} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage}"
            );
        }

        public void RequisitionCreated(Requisition requisition)
        {
            var order = requisition.Request.Order;
            CreateEmail(GetRecipiantsFromOrder(order),
                $"En rekvisition har registrerats på avrop {order.OrderNumber}",
                $"En rekvisition har registrerats på avrop {order.OrderNumber}"
            );
        }

        public void RequisitionApproved(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            List<string> receipents = new List<string>() { requisition.CreatedByUser.Email };
            //Broker
            var brokerAddress = requisition.Request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(brokerAddress))
            {
                //This will later be replaced with a check if the broker wants an email, or som other notification type.
                receipents.Add(brokerAddress);
            }
            CreateEmail(receipents, $"Rekvisition för avrop {orderNumber} har godkänts",
                $"Rekvisition för avrop {orderNumber} har godkänts" +
                $"\n\nKostnader att fakturera:\n\n{GetRequisitionPriceInformationForMail(requisition)}"
            );
        }

        public void RequisitionDenied(Requisition requisition)
        {
            string orderNumber = requisition.Request.Order.OrderNumber;
            List<string> receipents = new List<string>() { requisition.CreatedByUser.Email };
            //Broker
            var brokerAddress = requisition.Request.Ranking.Broker.EmailAddress;
            if (!string.IsNullOrEmpty(brokerAddress))
            {
                //This will later be replaced with a check if the broker wants an email, or som other notification type.
                receipents.Add(brokerAddress);
            }
            CreateEmail(receipents, $"Rekvisition för avrop {orderNumber} har underkänts",
                $"Rekvisition för avrop {orderNumber} har underkänts med följande meddelande:\n{requisition.DenyMessage}"
            );
        }

        public void RequestAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat avrop {orderNumber}",
                $"Svar på avrop {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Avropet har accepterats." +
                $"\n\nTolk:\n{request.Interpreter.User.CompleteContactInformation}");
        }

        public void RequestDeclinedByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har tackat nej till avrop {orderNumber}",
                 $"Svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till avropet med följande meddelande:\n{request.DenyMessage}");
        }

        public void RequestCancelledByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har avbokat avrop {orderNumber}",
                 $"Förmedling {request.Ranking.Broker.Name} har avbokat uppdraget för avrop {orderNumber} med meddelande:\n{request.CancelMessage}");
            //create email to interpreter
            CreateEmail(request.Interpreter.User.Email, $"Förmedling har avbokat ditt uppdrag för avrops-ID {request.Order.OrderNumber}",
                $"Förmedling { request.Ranking.Broker.Name} har avbokat ditt uppdrag för avrop {orderNumber} med meddelande:\n{request.CancelMessage}\n" +
                $"Uppdraget skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.");
        }

        public void RequestReplamentOrderAccepted(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                         $"Svar på ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Ersättningsuppdrag har accepterats." +
                                "Eventuellt förändrade svar finns som måste beaktas.");
                    break;
                case RequestStatus.Approved:
                    CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har accepterat ersättningsuppdrag {orderNumber}",
                        $"Ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har accepteras." +
                        "Inga förändrade krav finns, avropet är klart för utförande.");
                    //send mail to interpreter about changes replaced order => order
                    CreateEmail(request.Interpreter.User.Email, $"Tilldelat tolkuppdrag avrops-ID {orderNumber}",
                        $"Ditt tolkuppdrag {request.Order.ReplacingOrder.OrderNumber} hos {request.Order.CustomerOrganisation.Name} " +
                        $"från förmedling {request.Ranking.Broker.Name} har ersatts av ett nytt uppdrag: {orderNumber} " +
                        $"och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.");
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestReplamentOrderAccepted)} cannot send notifications on requests with status: {request.Status.ToString()}");
            }
        }

        public void RequestReplamentOrderDeclinedByBroker(Request request)
        {
            string orderNumber = request.Order.OrderNumber;

            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har tackat nej till ersättningsuppdrag {orderNumber}",
                $"Svar på ersättningsuppdrag {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} " +
                $"har tackat nej till ersättningsuppdrag med följande meddelande:\n{request.DenyMessage}");
            //send mail to interpreter about changes replaced order => order
            var cancelledRequest = request.Order.ReplacingOrder.Requests.Single(r => r.Ranking.BrokerId == request.Ranking.BrokerId && (
                r.Status == RequestStatus.CancelledByCreator ||
                r.Status == RequestStatus.CancelledByCreatorConfirmed ||
                r.Status == RequestStatus.CancelledByCreatorWhenApproved));
            CreateEmail(request.Interpreter.User.Email, $"Avbokat avrop avrops-ID {request.Order.ReplacingOrder.OrderNumber}",
                $"Ditt tolkuppdrag hos {cancelledRequest.Ranking.Broker.Name} har avbokats, med detta meddelande:\n {cancelledRequest.CancelMessage}\n" +
                $"Uppdraget har avrops-ID {request.Order.ReplacingOrder.OrderNumber} och skulle ha startat {request.Order.ReplacingOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}.");
        }

        public void RequestChangedInterpreter(Request request)
        {
            string orderNumber = request.Order.OrderNumber;
            CreateEmail(GetRecipiantsFromOrder(request.Order), $"Förmedling har bytt tolk på avrop {orderNumber}",
                $"Nytt svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk på avropet.\n" +
                $"\nTolk:\n{request.Interpreter.User.CompleteContactInformation}\n\n" +
                (request.Order.AllowMoreThanTwoHoursTravelTime ?
                    "Eventuellt förändrade krav finns som måste beaktas. Om byte av tolk på avropet inte godkänns/avslås så kommer systemet godkänna avropet automatiskt " +
                    $"{_options.HoursToApproveChangeInterpreterRequests} timmar före uppdraget startar förutsatt att avropet tidigare haft status godkänt." :
                    "Inga förändrade krav finns, avropet behåller sin nuvarande status."));
        }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User)
        {
            string orderNumber = request.Order.OrderNumber;
            //Interpreter
            NotifyInterpreterOnAssignenment(request);
            //Broker
            CreateEmail(request.Ranking.Broker.EmailAddress, $"Byte av tolk godkänt på Avrops-id {orderNumber}",
                $"Bytet av tolk har godkänts på order Avrops-id {orderNumber}"
            );
            //Creator
            switch (changeOrigin)
            {
                case InterpereterChangeAcceptOrigin.SystemRule:
                    CreateEmail(request.Order.CreatedByUser.Email,
                        $"Svar på avrop med avrops-ID {request.Order.OrderNumber} har godkänts av systemet",
                        $"Svar på avrop {request.Order.OrderNumber} där tolk har bytts ut har godkänts av systemet då uppdraget " +
                        $"startar inom {_options.HoursToApproveChangeInterpreterRequests} timmar. " +
                        $"Uppdraget startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}."
                    );
                    break;
                case InterpereterChangeAcceptOrigin.NoNeedForUserAccept:
                    CreateEmail(request.Order.CreatedByUser.Email, $"Förmedling har bytt tolk på avrop {orderNumber}",
                        $"Nytt svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk på avropet.\n" +
                        $"\nTolk:\n{request.Interpreter.User.CompleteContactInformation}\n\n" +
                        "Inga förändrade krav finns, avropet behåller sin nuvarande status."
                    );
                    break;
                case InterpereterChangeAcceptOrigin.User:
                    //No mail to customer if it was the customer that accepted.
                    break;
                default:
                    throw new NotImplementedException($"{nameof(RequestChangedInterpreterAccepted)} faild to send mail to customer. {changeOrigin.ToString()} is not a handled {nameof(InterpereterChangeAcceptOrigin)}");
            }
        }

        public void NotifyInterpreterOnAssignenment(Request request)
        {
            CreateEmail(request.Interpreter.User.Email, $"Tilldelat tolkuppdrag avrops-ID {request.Order.OrderNumber}",
               $"Du har fått ett tolkuppdrag hos {request.Order.CustomerOrganisation.Name} från förmedling {request.Ranking.Broker.Name}. " +
               $"Uppdraget har avrops-ID {request.Order.OrderNumber} och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.");
        }

        public void CreateEmail(string recipient, string subject, string body)
        {
            CreateEmail(new[] { recipient }, subject, body);
        }

        private void CreateEmail(IEnumerable<string> recipients, string subject, string body)
        {
            foreach (string recipient in recipients)
            {
                _dbContext.Add(new OutboundEmail(
                    recipient,
                    subject,
                    $"{body}\n\nDetta mejl går inte att svara på.",
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
                invoiceInfo += $"{priceInfo.HeaderDescription}\n\n";
                foreach (DisplayPriceRow dpr in priceInfo.DisplayPriceRows)
                {
                    invoiceInfo += $"{dpr.Description}:\n{dpr.Price.ToString("#,0.00 SEK")}\n\n";
                }
                invoiceInfo += $"Summa totalt att fakturera: {priceInfo.TotalPrice.ToString("#,0.00 SEK")}";
                return invoiceInfo;
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


        private BrokerNotificationSettings GetBrokerNotificationSettings(int brokerId, NotificationType type)
        {
            var roleId = _dbContext.Roles.Single(r => r.Name == "NotificationHandler").Id;
            var user = _dbContext.Users.SingleOrDefault(u => u.BrokerId == brokerId && u.Roles.Any(r => r.RoleId == roleId));
            //This should be cached, and gotten from the settings table called NotificationSettings, GetSettings(userId, type)
            if (user != null)
            {
                return new BrokerNotificationSettings
                {
                    EmailAddress = user.Email,
                    SendEmail = false,
                    CallWebhook = true,
                    Webhook = "https://localhost:5001/Request/Created",
                    RecipientUserId = user.Id,
                };
            }
            else
            {
                return new BrokerNotificationSettings
                {
                    SendEmail = true,
                    CallWebhook = false,
                };
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

    }
}
