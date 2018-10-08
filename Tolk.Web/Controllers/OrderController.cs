using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
        private readonly TolkOptions _options;
        private readonly EventLogService _eventLog;

        public OrderController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            IAuthorizationService authorizationService,
            RankingService rankingService,
            OrderService orderService,
            DateCalculationService dateCalculationService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            IOptions<TolkOptions> options,
            EventLogService eventLog
            )
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _authorizationService = authorizationService;
            _rankingService = rankingService;
            _orderService = orderService;
            _dateCalculationService = dateCalculationService;
            _clock = clock;
            _logger = logger;
            _options = options.Value;
            _eventLog = eventLog;
        }

        public IActionResult List(OrderFilterModel model)
        {
            var orders = _dbContext.Orders
                .Include(o => o.Language)
                .Include(o => o.CreatedByUser)
                .Include(o => o.Region)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Ranking)
                    .ThenInclude(r => r.Broker)
                .Where(o => o.CustomerOrganisationId == User.TryGetCustomerOrganisationId());
            var isSuperUser = User.IsInRole(Roles.SuperUser);
            if (!isSuperUser)
            {
                orders = orders.Where(o => o.CreatedBy == User.GetUserId());
            }

            // Filters
            if (model != null)
            {
                orders = model.Apply(orders);
            }
            else
            {
                model = new OrderFilterModel();
            }

            model.IsSuperUser = isSuperUser;

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
                        CreatorName = o.CreatedByUser.FullName,
                        BrokerName = o.Requests.Where(r =>
                            r.Status == RequestStatus.Created ||
                            r.Status == RequestStatus.Received ||
                            r.Status == RequestStatus.Accepted ||
                            r.Status == RequestStatus.Approved ||
                            r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                            .Select(r => r.Ranking.Broker.Name).FirstOrDefault(),
                        Action = nameof(View)
                    })

                });
        }

        public async Task<IActionResult> View(int id)
        {
            //Get order model from db
            Order order = GetOrder(id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                var now = _clock.SwedenNow;
                //TODO: Handle this better. Preferably with a list that you can use contains on
                var request = order.Requests.SingleOrDefault(r =>
                        r.Status != RequestStatus.InterpreterReplaced &&
                        r.Status != RequestStatus.DeniedByTimeLimit &&
                        r.Status != RequestStatus.DeniedByCreator &&
                        r.Status != RequestStatus.DeclinedByBroker);
                var model = OrderModel.GetModelFromOrder(order, request?.RequestId);
                model.AllowOrderCancellation = request != null &&
                    order.StartAt > _clock.SwedenNow &&
                    (await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded;
                model.AllowReplacementOnCancel = model.AllowOrderCancellation &&
                    request.Status == RequestStatus.Approved &&
                    _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(now.DateTime, order.StartAt.DateTime) < 2 &&
                    !request.Order.ReplacingOrderId.HasValue;
                model.OrderCalculatedPriceInformationModel = GetPriceinformationToDisplay(order);
                model.RequestStatus = request?.Status;
                model.BrokerName = request?.Ranking.Broker.Name;
                model.BrokerOrganizationNumber = request?.Ranking.Broker.OrganizationNumber;
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                //don't use AnsweredBy since request for replacement order can have interpreter etc but not is answered
                model.ActiveRequestIsAnswered = request?.InterpreterId != null;
                if (model.ActiveRequestIsAnswered)
                {
                    model.CancelMessage = request.CancelMessage;
                    model.ActiveRequestPriceInformationModel = GetPriceinformationToDisplay(request);
                    model.RequestId = request.RequestId;
                    model.AnsweredBy = request.AnsweringUser?.CompleteContactInformation;
                    model.ExpectedTravelCosts = request.ExpectedTravelCosts ?? 0;
                    model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                    model.InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel)request.CompetenceLevel;
                    model.InterpreterName = _dbContext.Requests
                        .Include(r => r.Interpreter)
                        .ThenInclude(i => i.User)
                        .Single(r => r.RequestId == request.RequestId).Interpreter?.User.CompleteContactInformation;
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
                    model.RequestAttachmentListModel = new AttachmentListModel
                    {
                        AllowDelete = false,
                        AllowDownload = true,
                        AllowUpload = false,
                        Title = "Bifogade filer från förmedling",
                        Files = request.Attachments.Select(a => new FileModel
                        {
                            Id = a.Attachment.AttachmentId,
                            FileName = a.Attachment.FileName,
                            Size = a.Attachment.Blob.Length
                        }).ToList()
                    };
                }
                model.EventLog = EventLogModel.GetModel(_eventLog.GetLogs(order));
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Replace(int replacingOrderId, string cancelMessage)
        {
            var order = GetOrder(replacingOrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                ReplaceOrderModel model = Mapper.Map<ReplaceOrderModel>(OrderModel.GetModelFromOrder(order));
                model.ReplacedTimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                };
                model.OrderId = null;
                model.ReplacingOrderNumber = order.OrderNumber;
                model.ReplacingOrderId = replacingOrderId;
                model.CancelMessage = cancelMessage;
                //Set the Files-list and the used FileGroupKey
                List<FileModel> files = order.Attachments.Select(a => new FileModel
                {
                    Id = a.Attachment.AttachmentId,
                    FileName = a.Attachment.FileName,
                    Size = a.Attachment.Blob.Length
                }).ToList();
                model.Files = files.Count() > 0 ? files : null;
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Replace(ReplaceOrderModel model)
        {
            if (ModelState.IsValid)
            {
                Order order = GetOrder(model.ReplacingOrderId.Value);
                if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
                {
                    using (var trn = await _dbContext.Database.BeginTransactionAsync())
                    {
                        //TODO: Handle this better. Preferably with a list that you can use contains on,
                        //OR a property on order!! Should probably throw null pointer exeption if Requests are not included.
                        var request = order.Requests.SingleOrDefault(r =>
                        r.Status == RequestStatus.Created ||
                        r.Status == RequestStatus.Received ||
                        r.Status == RequestStatus.Accepted ||
                        r.Status == RequestStatus.Approved ||
                        r.Status == RequestStatus.AcceptedNewInterpreterAppointed);

                        Order replacementOrder = CreateNewOrder();
                        var replacingRequest = new Request(request, _orderService.CalculateExpiryForNewRequest(model.TimeRange.StartDateTime), _clock.SwedenNow);
                        order.MakeCopy(replacementOrder);
                        model.UpdateOrder(replacementOrder, true);
                        replacementOrder.Requests.Add(replacingRequest);
                        _dbContext.Add(replacementOrder);
                        request.Cancel(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage, isReplaced: true);

                        replacementOrder.Requirements = order.Requirements.Select(r => new OrderRequirement
                        {
                            Description = r.Description,
                            IsRequired = r.IsRequired,
                            RequirementType = r.RequirementType,
                            RequirementAnswers = r.RequirementAnswers
                            .Where(a => a.RequestId == request.RequestId)
                            .Select(a => new OrderRequirementRequestAnswer
                            {
                                Answer = a.Answer,
                                CanSatisfyRequirement = a.CanSatisfyRequirement,
                                RequestId = replacingRequest.RequestId
                            }).ToList(),
                        }).ToList();

                        //Genarate new price rows from current times, might be subject to change!!!
                        _orderService.CreatePriceInformation(replacementOrder);
                        var brokerEmail = _dbContext.Brokers.Single(b => b.BrokerId == request.Ranking.BrokerId).EmailAddress;
                        if (!string.IsNullOrEmpty(brokerEmail))
                        {
                            _dbContext.Add(new OutboundEmail(
                                request.Ranking.Broker.EmailAddress,
                                $"Avrop {order.OrderNumber} har avbokats, med ersättningsuppdrag: {replacementOrder.OrderNumber}",
                                $"\tOrginal Start: {order.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                                $"\tOrginal Slut: {order.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                                $"\tErsättning Start: {replacementOrder.StartAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                                $"\tErsättning Slut: {replacementOrder.EndAt.ToString("yyyy-MM-dd HH:mm")}\n" +
                                $"\tTolk: {request.Interpreter.User.FullName}, e-post: {request.Interpreter.User.Email}\n" +
                                $"\tSvara senast: {replacingRequest.ExpiresAt.ToString("yyyy-MM-dd HH:mm")}\n\n" +
                                "Detta mejl går inte att svara på.",
                                _clock.SwedenNow));
                        }
                        else
                        {
                            _logger.LogInformation("No mail sent to broker {brokerId}, it has no email set.",
                               request.Ranking.BrokerId);
                        }

                        _dbContext.SaveChanges();
                        //Close the replaced order as cancelled
                        trn.Commit();
                        return RedirectToAction(nameof(View), new { id = replacementOrder.OrderId });
                    }
                }
            }
            return View(model);
        }

        public IActionResult Add()
        {
            var model = new OrderModel()
            {
                SystemTime = (long) _clock.SwedenNow.DateTime.ToUnixTimestamp(),

            };
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Add(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                using (var trn = await _dbContext.Database.BeginTransactionAsync())
                {
                    Order order = CreateNewOrder();

                    model.UpdateOrder(order);
                    _dbContext.Add(order);
                    _dbContext.SaveChanges(); // Save changes to get id for event log
                    var user = _dbContext.Users
                        .Include(u => u.CustomerOrganisation)
                        .Single(u => u.Id == order.CreatedBy);
                    _eventLog.Push(order.OrderId, ObjectType.Order, "Avrop skapad", user.FullName, user.CustomerOrganisation.Name);

                    await _orderService.CreateRequest(order, latestAnswerBy: model.LatestAnswerBy);
                    _orderService.CreatePriceInformation(order);

                    _dbContext.SaveChanges();
                    trn.Commit();
                    return RedirectToAction(nameof(View), new { id = order.OrderId });
                }
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
                if (model.AddReplacementOrder)
                {
                    //Forward the message
                    return RedirectToAction(nameof(Replace), new { replacingOrderId = model.OrderId, cancelMessage = model.CancelMessage });
                }
                var now = _clock.SwedenNow;
                //If this is an approved request, and the cancellation is done to late, a requisition with full compensation will be created
                bool createFullCompensationRequisition = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(now.DateTime, request.Order.StartAt.DateTime) < 2;
                bool isApprovedRequest = request.Status == RequestStatus.Approved;
                request.Cancel(now, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage, createFullCompensationRequisition);

                CreateEmailOnOrderCancellation(request, isApprovedRequest, createFullCompensationRequisition);

                _dbContext.SaveChanges();
                return RedirectToAction(nameof(View), new { id = model.OrderId });
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

            if ((await _authorizationService.AuthorizeAsync(User, request.Order, Policies.View)).Succeeded)
            {
                request.Status = RequestStatus.CancelledByBrokerConfirmed;
                request.CancelConfirmedAt = _clock.SwedenNow;
                request.CancelConfirmedBy = User.GetUserId();
                request.ImpersonatingCancelConfirmer = User.TryGetImpersonatorId();
                request.Order.Status = OrderStatus.CancelledByBrokerConfirmed;
                _dbContext.SaveChanges();
                return RedirectToAction("Index", "Home", new { message = "Avbokning är bekräftad" });
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

                await _orderService.CreateRequest(order, request);

                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }

            return Forbid();
        }

        private PriceInformationModel GetPriceinformationToDisplay(Request request)
        {
            if (request.PriceRows == null)
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt avropssvar",
                UseDisplayHideInfo = true
            };
        }

        private PriceInformationModel GetPriceinformationToDisplay(Order order)
        {
            if (order.PriceRows == null)
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(order.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt ursprungligt avrop",
                UseDisplayHideInfo = true
            };
        }

        private Order CreateNewOrder()
        {
            return new Order
            {
                Status = OrderStatus.Requested,
                CreatedBy = User.GetUserId(),
                CreatedAt = _clock.SwedenNow,
                CustomerOrganisationId = User.GetCustomerOrganisationId(),
                ImpersonatingCreator = User.TryGetImpersonatorId(),
                Requirements = new List<OrderRequirement>(),
                InterpreterLocations = new List<OrderInterpreterLocation>(),
                PriceRows = new List<OrderPriceRow>(),
                Requests = new List<Request>(),
                CompetenceRequirements = new List<OrderCompetenceRequirement>()
            };
        }

        private Order GetOrder(int id)
        {
            return _dbContext.Orders
                .Include(o => o.ReplacedByOrder)
                .Include(o => o.ReplacingOrder)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Region)
                .Include(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.InterpreterLocations)
                .Include(o => o.CompetenceRequirements)
                .Include(o => o.Attachments).ThenInclude(o => o.Attachment)
                .Include(o => o.Requirements)
                    .ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Ranking)
                    .ThenInclude(r => r.Broker)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Complaints)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.Interpreter)
                    .ThenInclude(i => i.User)
                .Include(o => o.Requests)
                    .ThenInclude(r => r.AnsweringUser)
                .Include(o => o.Requests).ThenInclude(r => r.Attachments)
                .ThenInclude(r => r.Attachment)
                .Single(o => o.OrderId == id);
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

        private void CreateEmailOnOrderCancellation(Request request, bool requestWasApproved, bool createFullCompensationRequisition)
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
                        (createFullCompensationRequisition ? "\nUppdraget får faktureras eftersom avbokningen skedde så nära inpå." : "\nFörmedlingsavgift utgår som får faktureras") +
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
                        (createFullCompensationRequisition ? "\nUppdraget får faktureras eftersom avbokningen skedde så nära inpå." : "\nFörmedlingsavgift utgår som får faktureras") +
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
