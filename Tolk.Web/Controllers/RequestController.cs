using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Broker)]
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly InterpreterService _interpreterService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService,
            InterpreterService interpreterService,
            PriceCalculationService priceCalculationService,
            ILogger<RequisitionController> logger,
            IOptions<TolkOptions> options
)
        {
            _dbContext = dbContext;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
            _interpreterService = interpreterService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
            _options = options.Value;
        }

        public IActionResult List(RequestFilterModel model)
        {
            bool isCustomer = User.TryGetCustomerOrganisationId().HasValue;

            var items = _dbContext.Requests.Include(r => r.Order)
                        .Where(r => r.Ranking.Broker.BrokerId == User.GetBrokerId() && r.Status != RequestStatus.InterpreterReplaced)
                        .SelectRequestListItemModel(isCustomer);
            // Filters
            if (model != null)
            {
                items = model.Apply(items);
            }

            return View(
                new RequestListModel
                {
                    Items = items,
                    FilterModel = model
                });
        }

        public async Task<IActionResult> View(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.PriceRows)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Requisitions)
                .Include(r => r.Complaints)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return View(GetModel(request));
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.PriceRows)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (request.Status == RequestStatus.Created)
                {
                    request.Received(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                }
                RequestModel model = GetModel(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Change(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.PriceRows)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Include(r => r.Ranking)
                .Include(r => r.RequirementAnswers)
                .Single(o => o.RequestId == id);
            RequestModel model = GetModel(request);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (request.Status == RequestStatus.Approved || request.Status == RequestStatus.Accepted)
                {
                    model.Status = RequestStatus.AcceptedNewInterpreterAppointed;
                    model.ExpectedTravelCosts = 0;
                }
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                return View("Process", model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Accept(RequestAcceptModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
		    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
		    .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Interpreter).ThenInclude(i => i.User)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.PriceRows)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)

                    .Single(o => o.RequestId == model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    //if change interpreter or else if not a replacementorder
                    if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed || (!request.Order.ReplacingOrderId.HasValue && model.Status != RequestStatus.AcceptedNewInterpreterAppointed))
                    {
                        string sendExtraEmailToInterpreter = string.Empty;
                        int interpreterId = model.InterpreterId.Value;
                        if (interpreterId == SelectListService.NewInterpreterId)
                        {
                            interpreterId = await _interpreterService.GetInterpreterId(
                                request.Ranking.BrokerId,
                                model.NewInterpreterEmail);
                        }
                        if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                        {
                            CreateNewRequestForReplacedInterpreter(request, model, interpreterId);
                            if (request.Status == RequestStatus.Approved && !request.Order.AllowMoreThanTwoHoursTravelTime)
                            {
                                sendExtraEmailToInterpreter = _dbContext.Users.Single(u => u.InterpreterId == interpreterId).Email;
                            }
                            request.Status = RequestStatus.InterpreterReplaced;
                        }
                        else
                        {
                            request.Accept(
                                _clock.SwedenNow,
                                User.GetUserId(),
                                User.TryGetImpersonatorId(),
                                interpreterId,
                                model.ExpectedTravelCosts,
                                model.InterpreterLocation,
                                model.InterpreterCompetenceLevel,
                                model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                                {
                                    RequestId = request.RequestId,
                                    OrderRequirementId = ra.OrderRequirementId,
                                    Answer = ra.Answer,
                                    CanSatisfyRequirement = ra.CanMeetRequirement
                                }),
                                model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }).ToList(),
                                GetPrices(request, model.InterpreterCompetenceLevel.Value)
                            );
                        }
                        CreateEmailOnRequestAction(request, sendExtraEmailToInterpreter);
                    }
                    else
                    {
                        request.AcceptReplacementOrder(
                            _clock.SwedenNow,
                            User.GetUserId(),
                            User.TryGetImpersonatorId(),
                            model.ExpectedTravelCosts,
                            GetPrices(request, (CompetenceAndSpecialistLevel)request.CompetenceLevel)
                        );

                        CreateEmailOnProcessReplacementOrder(request);
                    }
                    _dbContext.SaveChanges();
                    return RedirectToAction("Index", "Home", new { message = model.Status == RequestStatus.AcceptedNewInterpreterAppointed ? "Tolk har bytts ut för uppdraget" : "Svar har skickats" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Process), new { id = model.RequestId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Cancel(RequestCancelModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Requisitions)
                .Include(r => r.PriceRows)
                .Single(r => r.RequestId == model.RequestId && r.Status == RequestStatus.Approved);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded)
                {
                    request.CancelByBroker(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                    CreateEmailOnRequestAction(request, request.Interpreter.User.Email);
                    _dbContext.SaveChanges();
                    return RedirectToAction("Index", "Home", new { message = "Avbokning har genomförts" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.RequestId });
        }

        private void CreateNewRequestForReplacedInterpreter(Request request, RequestAcceptModel model, int interpreterId)
        {
            Request newRequest = new Request(request.Ranking, request.ExpiresAt);
            newRequest.OrderId = request.OrderId;
            newRequest.Status = RequestStatus.AcceptedNewInterpreterAppointed;
            request.Order.Requests.Add(newRequest);
            _dbContext.SaveChanges();
            newRequest.ReplaceInterpreter(_clock.SwedenNow,
                User.GetUserId(),
                User.TryGetImpersonatorId(),
                interpreterId,
                model.ExpectedTravelCosts,
                model.InterpreterLocation,
                model.InterpreterCompetenceLevel,
                model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                {
                    RequestId = newRequest.RequestId,
                    OrderRequirementId = ra.OrderRequirementId,
                    Answer = ra.Answer,
                    CanSatisfyRequirement = ra.CanMeetRequirement
                }),
                model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }).ToList(),
                GetPrices(request, model.InterpreterCompetenceLevel.Value),
                !request.Order.AllowMoreThanTwoHoursTravelTime,
                request
                 );
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Decline(RequestDeclineModel model)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Single(r => r.RequestId == model.RequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                request.Status = RequestStatus.DeclinedByBroker;
                request.AnswerDate = _clock.SwedenNow;
                request.AnsweredBy = User.GetUserId();
                request.ImpersonatingAnsweredBy = User.TryGetImpersonatorId();
                request.DenyMessage = model.DenyMessage;
                if (!request.Order.ReplacingOrderId.HasValue)
                {
                    request.Order.Status = OrderStatus.Requested;
                    await _orderService.CreateRequest(request.Order);
                    CreateEmailOnRequestAction(request, string.Empty);
                }
                else
                {
                    request.Order.Status = OrderStatus.NoBrokerAcceptedOrder;
                    CreateEmailOnProcessReplacementOrder(request);
                }
                _dbContext.SaveChanges();
                return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
            }

            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmCancellation(int requestId)
        {
            var request = _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .Single(r => r.RequestId == requestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                request.Status = RequestStatus.CancelledByCreatorConfirmed;
                request.CancelConfirmedAt = _clock.SwedenNow;
                request.CancelConfirmedBy = User.GetUserId();
                request.ImpersonatingCancelConfirmer = User.TryGetImpersonatorId();
                request.Order.Status = OrderStatus.CancelledByCreatorConfirmed;
                _dbContext.SaveChanges();
                return RedirectToAction("Index", "Home", new { message = "Avbokning är bekräftad" });
            }

            return Forbid();
        }

        private PriceInformation GetPrices(Request request, CompetenceAndSpecialistLevel competenceLevel)
        {
            return _priceCalculationService.GetPrices(
                            request.Order.StartAt,
                            request.Order.EndAt,
                            EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(competenceLevel),
                            request.Order.CustomerOrganisation.PriceListType,
                            request.Ranking.BrokerFee);
        }

        private RequestModel GetModel(Request request)
        {
            var model = RequestModel.GetModelFromRequest(request);
            model.CalculatedPrice = GetPrices(request, OrderService.SelectCompetenceLevelForPriceEstimation(model.OrderModel.RequestedCompetenceLevels)).TotalPrice;
            if (request.InterpreterLocation != null)
            {
                model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
            }
            
            model.BrokerId = request.Ranking.BrokerId;
            model.AllowInterpreterChange = ((request.Status == RequestStatus.Approved || request.Status == RequestStatus.Accepted || request.Status == RequestStatus.AcceptedNewInterpreterAppointed) && request.Order.StartAt > _clock.SwedenNow);
            model.AllowCancellation = request.Order.StartAt > _clock.SwedenNow && _authorizationService.AuthorizeAsync(User, request, Policies.Cancel).Result.Succeeded;
            return model;
        }

        private void CreateEmailOnRequestAction(Request request, string sendExtraMailToInterpreter)
        {
            string receipent = request.Order.CreatedByUser.Email;
            string contactPersonEmail = request.Order.ContactPersonUser?.Email;
            string subject;
            string body;
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    subject = $"Förmedling har accepterat avrop {orderNumber}";
                    body = $"Svar på avrop {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Avropet har accepterats.";
                    break;
                case RequestStatus.DeclinedByBroker:
                    subject = $"Förmedling har tackat nej till avrop {orderNumber}";
                    body = $"Svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till avropet med följande meddelande:\n{request.DenyMessage}";
                    break;
                case RequestStatus.InterpreterReplaced:
                    subject = $"Förmedling har bytt tolk på avrop {orderNumber}";
                    body = $"Nytt svar på avrop {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har bytt tolk på avropet.\n";
                    body += request.Order.AllowMoreThanTwoHoursTravelTime ? $"Eventuellt förändrade krav finns som måste beaktas. Om byte av tolk på avropet inte godkänns/avslås så kommer systemet godkänna avropet automatiskt {_options.HoursToApproveChangeInterpreterRequests} timmar före uppdraget startar förutsatt att avropet tidigare haft status godkänt." : "Inga förändrade krav finns, avropet behåller sin nuvarande status.";
                    //create email to new interpreter
                    if (!string.IsNullOrEmpty(sendExtraMailToInterpreter))
                    {
                        _dbContext.Add(new OutboundEmail(
                        sendExtraMailToInterpreter,
                        $"Tilldelat tolkuppdrag avrops-ID {request.Order.OrderNumber}",
                        $"Du har fått ett tolkuppdrag hos {request.Order.CustomerOrganisation.Name} från förmedling {request.Ranking.Broker.Name}. Uppdraget har avrops-ID {request.Order.OrderNumber} och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                    }
                    break;
                case RequestStatus.CancelledByBroker:
                    subject = $"Förmedling har avbokat avrop {orderNumber}";
                    body = $"Förmedling {request.Ranking.Broker.Name} har avbokat uppdraget för avrop {orderNumber} med meddelande:\n{request.CancelMessage}";
                    //create email to interpreter
                    if (!string.IsNullOrEmpty(sendExtraMailToInterpreter))
                    {
                        _dbContext.Add(new OutboundEmail(
                        sendExtraMailToInterpreter,
                        $"Förmedling har avbokat ditt uppdrag för avrops-ID {request.Order.OrderNumber}",
                        $"Förmedling { request.Ranking.Broker.Name} har avbokat ditt uppdrag för avrop {orderNumber} med meddelande:\n{request.CancelMessage}\nUppdraget skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    subject,
                    body +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
            }
            else
            {
                _logger.LogInformation($"No email sent for request action {request.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
            //create email for contact person/delegate if exists
            if (!string.IsNullOrEmpty(contactPersonEmail))
            {
                _dbContext.Add(new OutboundEmail(
                    contactPersonEmail,
                    subject,
                    body +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
            }
        }

        private void CreateEmailOnProcessReplacementOrder(Request request)
        {
            string receipent = request.Order.CreatedByUser.Email;
            string subject;
            string body;
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Accepted:
                    subject = $"Förmedling har accepterat ersättningsuppdrag {orderNumber}";
                    body = $"Svar på ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har inkommit. Ersättningsuppdrag har accepterats." +
                        "Eventuellt förändrade svar finns som måste beaktas.";
                    break;
                case RequestStatus.Approved:
                    subject = $"Förmedling har accepterat ersättningsuppdrag {orderNumber}";
                    body = $"Ersättningsuppdrag {orderNumber} från förmedling {request.Ranking.Broker.Name} har accepteras." +
                        "Inga förändrade krav finns, avropet är klart för utförande.";
                    //send mail to interpreter about changes replaced order => order
                    if (!string.IsNullOrEmpty(request.Interpreter.User.Email))
                    {
                        _dbContext.Add(new OutboundEmail(
                        request.Interpreter.User.Email,
                        $"Tilldelat tolkuppdrag avrops-ID {request.Order.OrderNumber}",
                        $"Ditt tolkuppdrag {request.Order.ReplacingOrder.OrderNumber} hos {request.Order.CustomerOrganisation.Name} från förmedling {request.Ranking.Broker.Name} har ersatts av ett nytt uppdrag: {request.Order.OrderNumber} och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                    }
                    break;
                case RequestStatus.DeclinedByBroker:
                    subject = $"Förmedling har tackat nej till ersättningsuppdrag {orderNumber}";
                    body = $"Svar på ersättningsuppdrag {orderNumber} har inkommit. Förmedling {request.Ranking.Broker.Name} har tackat nej till ersättningsuppdrag med följande meddelande:\n{request.DenyMessage}";
                    //send mail to interpreter about cancelled order (the replaced one)
                    if (!string.IsNullOrEmpty(request.Interpreter.User.Email))
                    {
                        var cancelMessage = request.Order.ReplacingOrder.Requests.Single(r => r.Ranking.BrokerId == request.Ranking.BrokerId && (
                            r.Status == RequestStatus.CancelledByCreator ||
                            r.Status == RequestStatus.CancelledByCreatorConfirmed ||
                            r.Status == RequestStatus.CancelledByCreatorWhenApproved)).CancelMessage;
                        _dbContext.Add(new OutboundEmail(
                        request.Interpreter.User.Email,
                            $"Avbokat avrop avrops-ID {orderNumber}",
                            $"Ditt tolkuppdrag hos {request.Order.ReplacingOrder.OrderNumber} har avbokats, med detta meddelande:\n {cancelMessage}\n" +
                            $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                            "\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                    }
                    else
                    {
                        _logger.LogInformation($"No email sent to interpreter when cancelling {request.Order.ReplacingOrder.OrderNumber}. No email is set for user.");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    subject,
                    body +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
            }
            else
            {
                _logger.LogInformation($"No email sent for request action {request.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }
    }
}
