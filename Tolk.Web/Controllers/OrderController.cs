using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Customer)]
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly RankingService _rankingService;
        private readonly OrderService _orderService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;

        public OrderController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            IAuthorizationService authorizationService,
            RankingService rankingService,
            OrderService orderService,
            DateCalculationService dateCalculationService,
            ISwedishClock clock,
            ILogger<OrderController> logger)
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _authorizationService = authorizationService;
            _rankingService = rankingService;
            _orderService = orderService;
            _dateCalculationService = dateCalculationService;
            _clock = clock;
            _logger = logger;
        }

        public IActionResult List(OrderFilterModel model)
        {
            var orders = _dbContext.Orders
                .Include(o => o.Language)
                .Include(o => o.Region)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Ranking)
                    .ThenInclude(r => r.Broker)
                .Where(o => o.CreatedBy == User.GetUserId());

            // Filters
            if (model != null)
            {
                orders = model.Apply(orders);
            }

            return View(
                new OrderListModel
                {
                    FilterModel = model,
                    Items = orders.Select(o => new OrderListItemModel
                    {
                        OrderId = o.OrderId,
                        Language = o.OtherLanguage ?? o.Language.Name ?? "(Tolkanvändarutbildning)",
                        OrderNumber = o.OrderNumber.ToString(),
                        RegionName = o.Region.Name,
                        Start = o.StartAt,
                        End = o.EndAt,
                        Status = o.Status,
                        BrokerName = o.Requests.Where(r =>
                            r.Status == RequestStatus.Created ||
                            r.Status == RequestStatus.Received ||
                            r.Status == RequestStatus.Accepted ||
                            r.Status == RequestStatus.Approved ||
                            r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                            .Select(r => r.Ranking.Broker.Name).FirstOrDefault()
                    })
                });
        }

        public async Task<IActionResult> View(int id)
        {
            //Get order model from db
            var order = _dbContext.Orders
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Region)
                .Include(o => o.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.InterpreterLocations)
                .Include(o => o.Requirements)
                    .ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Ranking)
                    .ThenInclude(r => r.Broker)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.PriceRows)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Complaints)
                .Single(o => o.OrderId == id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                //TODO: Handle this better. Preferably with a list that you can use contains on
                var request = order.Requests.SingleOrDefault(r =>
                        r.Status != RequestStatus.InterpreterReplaced &&
                        r.Status != RequestStatus.DeniedByTimeLimit &&
                        r.Status != RequestStatus.DeniedByCreator &&
                        r.Status != RequestStatus.DeclinedByBroker);
                var model = OrderModel.GetModelFromOrder(order, request?.RequestId);
                model.AllowOrderCancellation = request != null && order.StartAt > _clock.SwedenNow && (await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded;
                model.RequestStatus = request?.Status;
                model.BrokerName = request?.Ranking.Broker.Name;
                if (model.ActiveRequestIsAnswered)
                { 
                    model.CancelMessage = request.CancelMessage;
                    model.CalculatedPriceActiveRequest = request.PriceRows.Sum(p => p.TotalPrice);
                    model.RequestId = request.RequestId;
                    model.ExpectedTravelCosts = request.ExpectedTravelCosts ?? 0;
                    model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                    model.CompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel;
                    model.InterpreterName = _dbContext.Requests
                        .Include(r => r.Interpreter)
                        .ThenInclude(i => i.User)
                        .Single(r => r.RequestId == request.RequestId).Interpreter?.User.NormalizedEmail;
                    model.AllowComplaintCreation = !request.Complaints.Any() && 
                        (request.Status == RequestStatus.Approved || request.Status == RequestStatus.AcceptedNewInterpreterAppointed) &&
                        order.StartAt < _clock.SwedenNow && (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded;
                    var complaint = request.Complaints.FirstOrDefault();
                    if (complaint != null)
                    {
                        model.ComplaintId = complaint.ComplaintId;
                        model.ComplaintMessage = complaint.ComplaintMessage;
                        model.ComplaintStatus = complaint.Status;
                        model.ComplaintType = complaint.ComplaintType;
                    }
                }

                return View(model);
            }

            return Forbid();
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = _dbContext.Orders.Single(o => o.OrderId == id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                return View(OrderModel.GetModelFromOrder(order));
            }
            return Forbid();
        }

        public IActionResult Add()
        {
            return View("Edit");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Add(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                Order order;

                order = new Order
                {
                    Status = OrderStatus.Requested,
                    CreatedBy = User.GetUserId(),
                    CreatedAt = _clock.SwedenNow.DateTime,
                    CustomerOrganisationId = User.GetCustomerOrganisationId(),
                    ImpersonatingCreator = User.TryGetImpersonatorId(),
                    Requirements = new List<OrderRequirement>(),
                    InterpreterLocations = new List<OrderInterpreterLocation>(),
                    PriceRows = new List<OrderPriceRow>()
                };
                model.UpdateOrder(order);
                _dbContext.Add(order);

                await _orderService.CreateRequest(order);
                _orderService.CreatePriceInformation(order);

                _dbContext.SaveChanges();

                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }
            return View("Edit", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                var order = _dbContext.Orders.Single(o => o.OrderId == model.OrderId);

                if (!(await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
                {
                    return Forbid();
                }

                model.UpdateOrder(order);

                _dbContext.SaveChanges();

                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }

            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Approve(ProcessRequestModel model)
        {
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(o => o.CustomerOrganisation)
                .Single(o => o.OrderId == model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded)
            {
                var request = order.Requests.Single(r => r.RequestId == model.RequestId);

                request.Approve(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());

                _dbContext.SaveChanges();

                CreateEmailOnOrderRequestAction(request);
                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Cancel(CancelOrderModel model)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Requisitions)
                .Include(r => r.PriceRows)
                .Single(r => r.OrderId == model.OrderId &&
                    (
                        r.Status == RequestStatus.Created ||
                        r.Status == RequestStatus.Received ||
                        r.Status == RequestStatus.Accepted ||
                        r.Status == RequestStatus.Approved ||
                        r.Status == RequestStatus.AcceptedNewInterpreterAppointed
                ));
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded)
            {
                var now = _clock.SwedenNow;
                //If this is an approved request, and the cancellation is done to late, a requisition will be created.
                bool createRequisition = _dateCalculationService.GetWorkDaysBetween(now.Date, request.Order.StartAt.Date) < 2;
                bool isApprovedRequest = request.Status == RequestStatus.Approved;
                request.Cancel(now, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage, createRequisition);

                CreateEmailOnOrderCancellation(request, isApprovedRequest, createRequisition);

                _dbContext.SaveChanges();
                return RedirectToAction(nameof(View), new { id = model.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Deny(ProcessRequestModel model)
        {
            var order = await _dbContext.Orders.Include(o => o.Requests)
                .ThenInclude(r => r.Ranking)
                .SingleAsync(o => o.OrderId == model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded)
            {
                var request = order.Requests.Single(r => r.RequestId == model.RequestId);

                request.Deny(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);

                await _orderService.CreateRequest(order);

                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }

            return Forbid();
        }

        private void CreateEmailOnOrderRequestAction(Request request)
        {
            string receipent = request.Interpreter.User.Email;
            string subject;
            string body;
            string orderNumber = request.Order.OrderNumber;
            switch (request.Status)
            {
                case RequestStatus.Approved:
                    subject = $"Tilldelat tolkuppdrag avrops-ID {orderNumber}";
                    body = $"Du har fått ett tolkuppdrag hos {request.Order.CustomerOrganisation.Name} från förmedling {request.Ranking.Broker.Name}. Uppdraget har avrops-ID {orderNumber} och startar {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}.";
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
                _logger.LogInformation($"No email sent for orderrequest action {request.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }

        private void CreateEmailOnOrderCancellation(Request request, bool requestWasApproved, bool willGetInvoiced)
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
                        $"Ditt tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n {request.CancelMessage}\n" +
                        $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                        (willGetInvoiced ? "Uppdraget får faktureras eftersom avbokningen skedde så nära inpå." : string.Empty) +
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
                        $"Ert tolkuppdrag hos {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n {request.CancelMessage}\n" +
                        $"Uppdraget har avrops-ID {orderNumber} och skulle ha startat {request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}." +
                        (willGetInvoiced ? "\nUppdraget får faktureras eftersom avbokningen skedde så nära inpå." : string.Empty) +
                        "\n\nDetta mejl går inte att svara på.",
                        _clock.SwedenNow));
                }
                else
                {
                    _dbContext.Add(new OutboundEmail(
                        broker,
                        $"Avbokad förfrågan avrops-ID {request.Order.OrderNumber}",
                        $"Förfrågan från {request.Order.CustomerOrganisation.Name} har avbokats, med detta meddelande:\n {request.CancelMessage}\n" +
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
