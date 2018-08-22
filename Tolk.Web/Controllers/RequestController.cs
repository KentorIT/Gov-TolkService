using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Tolk.Web.Helpers;
using Tolk.Web.Authorization;
using Tolk.BusinessLogic.Services;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Utilities;
using Microsoft.Extensions.Logging;

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

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService,
            InterpreterService interpreterService,
            PriceCalculationService priceCalculationService,
            ILogger<RequisitionController> logger
)
        {
            _dbContext = dbContext;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
            _interpreterService = interpreterService;
            _priceCalculationService = priceCalculationService;
            _logger = logger;
        }

        public IActionResult List(RequestFilterModel model)
        {
            bool isCustomer = User.TryGetCustomerOrganisationId().HasValue;

            var items = _dbContext.Requests.Include(r => r.Order)
                        .Where(r => r.Ranking.Broker.BrokerId == User.GetBrokerId())
                        .Select(r => new RequestListItemModel
                        {
                            RequestId = r.RequestId,
                            Language = r.Order.OtherLanguage ?? r.Order.Language.Name ?? "(Tolkanvändarutbildning)",
                            OrderNumber = r.Order.OrderNumber.ToString(),
                            CustomerName = r.Order.CustomerOrganisation.Name,
                            RegionName = r.Order.Region.Name,
                            Start = r.Order.StartAt,
                            End = r.Order.EndAt,
                            ExpiresAt = r.ExpiresAt,
                            Status = r.Status,
                            Action = ((!isCustomer && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)) || (isCustomer && r.Status == RequestStatus.Accepted) ? nameof(Process) : nameof(View))
                        });
            // Filters
            if (model != null)
            {
                // OrderNumber
                items = !string.IsNullOrWhiteSpace(model.OrderNumber) 
                    ? items.Where(i => i.OrderNumber.Contains(model.OrderNumber)) 
                    : items;
                // Region
                items = model.RegionId.HasValue 
                    ? items.Where(i => i.RegionName == Region.Regions.Where(r => r.RegionId == model.RegionId).Single().Name) 
                    : items;
                // Customers
                items = model.CustomerOrganizationId.HasValue 
                    ? items.Where(i => i.CustomerName == _dbContext.CustomerOrganisations
                        .Where(c => c.CustomerOrganisationId == model.CustomerOrganizationId)
                        .Single().Name) 
                    : items;
                // Language
                items = model.LanguageId.HasValue
                    ? items.Where(i => i.Language == _dbContext.Languages.Where(l => l.LanguageId == model.LanguageId).Single().Name)
                    : items;
                // StartDateRange
                items = model.StartDateRange != null && model.StartDateRange.HasValue 
                    ? items.Where(i => model.StartDateRange.IsInRange(i.Start)) 
                    : items;
                // AnswerByDateRange
                items = model.AnswerByDateRange != null && model.AnswerByDateRange.HasValue
                    ? items.Where(i => i.ExpiresAt.HasValue && model.AnswerByDateRange.IsInRange(i.ExpiresAt.Value.Date))
                    : items;
                // Status
                if (model.Status.HasValue)
                {
                    items = model.Status.Value == RequestStatus.ToBeProcessedByBroker ? items.Where(r => r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) : items.Where(r => r.Status == model.Status);
                }
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
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Requisitions)
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
                .Include(r => r.Order).ThenInclude(r => r.PriceRows)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Ranking)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (request.Status == RequestStatus.Created)
                {
                    request.Received(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                }
                return View(GetModel(request));
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
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.PriceRows)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Single(o => o.RequestId == model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    int interpreterId = model.InterpreterId;
                    if (interpreterId == SelectListService.NewInterpreterId)
                    {
                        interpreterId = await _interpreterService.GetInterpreterId(
                            request.Ranking.BrokerId,
                            model.NewInterpreterEmail);
                    }

                    request.Accept(
                        _clock.SwedenNow,
                        User.GetUserId(),
                        User.TryGetImpersonatorId(),
                        interpreterId,
                        model.ExpectedTravelCosts,
                        model.InterpreterLocation,
                        model.CompetenceLevel,
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            RequestId = request.RequestId,
                            OrderRequirementId = ra.OrderRequirementId,
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement
                        }),
                        GetPrices(request, model.CompetenceLevel.Value)
                    );
                    _dbContext.SaveChanges();
                    CreateEmailOnRequestAction(request);
                    return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
                }
                return Forbid();
            }
            return View("Process", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Decline(RequestDeclineModel model)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Single(r => r.RequestId == model.RequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                request.Order.Status = OrderStatus.Requested;

                request.Status = RequestStatus.DeclinedByBroker;
                request.AnswerDate = _clock.SwedenNow;
                request.AnsweredBy = User.GetUserId();
                request.ImpersonatingAnsweredBy = User.TryGetImpersonatorId();
                request.DenyMessage = model.DenyMessage;
                await _orderService.CreateRequest(request.Order);

                _dbContext.SaveChanges();
                CreateEmailOnRequestAction(request);
                return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
            }

            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmCancallation(int requestId)
        {
            var request = _dbContext.Requests
                .Include(r => r.Ranking)
                .Single(r => r.RequestId == requestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                request.Status = RequestStatus.CancelledByCreatorConfirmed;
                request.CancelConfirmedAt = _clock.SwedenNow;
                request.CancelConfirmedBy = User.GetUserId();
                request.ImpersonatingCancelConfirmer = User.TryGetImpersonatorId();

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
            model.CalculatedPrice = GetPrices(request, model.OrderModel.RequiredCompetenceLevel).TotalPrice;
            if (request.InterpreterLocation != null)
            {
                model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
            }
            model.BrokerId = request.Ranking.BrokerId;
            return model;
        }

        private void CreateEmailOnRequestAction(Request request)
        {
            string receipent = request.Order.CreatedByUser.Email;
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
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for request action {request.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }
    }
}
