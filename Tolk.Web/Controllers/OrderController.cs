﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize(Policy = Policies.CustomerOrAdmin)]
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
        private readonly INotificationService _notificationService;
        private readonly UserManager<AspNetUser> _userManager;

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
            INotificationService notificationService,
            UserManager<AspNetUser> usermanager
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
            _notificationService = notificationService;
            _userManager = usermanager;
        }

        public IActionResult List(OrderFilterModel model)
        {
            if (model == null)
            {
                model = new OrderFilterModel();
            }
            var isAdmin = User.IsInRole(Roles.SystemAdministrator);
            var isCentralAdministrator = User.IsInRole(Roles.CentralAdministrator);
            model.IsCentralAdministrator = isCentralAdministrator;
            model.IsAdmin = isAdmin;
            var orders = _dbContext.Orders.Select(o => o);

            if (!isAdmin)
            {
                orders = orders.Where(o => o.CustomerOrganisationId == User.TryGetCustomerOrganisationId());
                if (!isCentralAdministrator)
                {
                    orders = orders.Where(o => o.CreatedBy == User.GetUserId() || o.ContactPersonId == User.GetUserId());
                }
            }

            // Filters
            orders = model.Apply(orders);

            return View(
                new OrderListModel
                {
                    FilterModel = model,
                    Items = orders.Select(o => new OrderListItemModel
                    {
                        OrderId = o.OrderId,
                        Language = o.OtherLanguage ?? o.Language.Name,
                        OrderNumber = o.OrderNumber.ToString(),
                        RegionName = o.Region.Name,
                        OrderDateAndTime = new TimeRange
                        {
                            StartDateTime = o.StartAt,
                            EndDateTime = o.EndAt
                        },
                        Status = o.Status,
                        CreatorName = o.CreatedByUser.FullName,
                        BrokerName = o.Requests.Where(r =>
                            r.Status == RequestStatus.Created ||
                            r.Status == RequestStatus.Received ||
                            r.Status == RequestStatus.Accepted ||
                            r.Status == RequestStatus.Approved ||
                            r.Status == RequestStatus.AcceptedNewInterpreterAppointed ||
                            r.Status == RequestStatus.AwaitingDeadlineFromCustomer)
                            .Select(r => r.Ranking.Broker.Name).FirstOrDefault(),
                        CustomerName = o.CustomerOrganisation.Name,
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
                    request.CanCancel &&
                    order.StartAt > _clock.SwedenNow &&
                    (await _authorizationService.AuthorizeAsync(User, order, Policies.Cancel)).Succeeded;
                model.AllowReplacementOnCancel = model.AllowOrderCancellation &&
                    request.Status == RequestStatus.Approved &&
                    _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(now.DateTime, order.StartAt.DateTime) < 2 &&
                    !request.Order.ReplacingOrderId.HasValue;
                model.AllowNoAnswerConfirmation = order.Status == OrderStatus.NoBrokerAcceptedOrder && !order.OrderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder) && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded;
                model.AllowConfirmCancellation = order.Status == OrderStatus.CancelledByBroker && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker) && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded;
                model.OrderCalculatedPriceInformationModel = GetPriceinformationToDisplay(order);
                model.RequestStatus = request?.Status;
                model.BrokerName = request?.Ranking.Broker.Name;
                model.BrokerOrganizationNumber = request?.Ranking.Broker.OrganizationNumber;
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.AllowUpdateExpiry = order.Status == OrderStatus.AwaitingDeadlineFromCustomer && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded;
                model.AllowEditContactPerson = order.Status != OrderStatus.CancelledByBroker && order.Status != OrderStatus.CancelledByCreator && order.Status != OrderStatus.NoBrokerAcceptedOrder && order.Status != OrderStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded;
                //don't use AnsweredBy since request for replacement order can have interpreter etc but not is answered
                model.ActiveRequestIsAnswered = request?.InterpreterBrokerId != null && (request?.Status != RequestStatus.Created && request?.Status != RequestStatus.Received);
                if (model.ActiveRequestIsAnswered)
                {
                    model.CancelMessage = request.CancelMessage;
                    model.ActiveRequestPriceInformationModel = GetPriceinformationToDisplay(request);
                    model.RequestId = request.RequestId;
                    model.AnsweredBy = request.AnsweringUser?.CompleteContactInformation;
                    model.ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0;
                    //There is no InterpreterLocation for replacement order if not answered yet
                    if (request.InterpreterLocation.HasValue)
                    {
                        model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                    }
                    model.InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel)request.CompetenceLevel;
                    model.InterpreterName = _dbContext.Requests
                        .Include(r => r.Interpreter)
                        .Single(r => r.RequestId == request.RequestId).Interpreter?.CompleteContactInformation;
                    model.AllowComplaintCreation = request.CanCreateComplaint &&
                        order.StartAt < _clock.SwedenNow && (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded;

                    model.RequestAttachmentListModel = new AttachmentListModel
                    {
                        AllowDelete = false,
                        AllowDownload = true,
                        AllowUpload = false,
                        Title = "Bifogade filer från förmedling",
                        DisplayFiles = request.Attachments.Select(a => new FileModel
                        {
                            Id = a.Attachment.AttachmentId,
                            FileName = a.Attachment.FileName,
                            Size = a.Attachment.Blob.Length
                        }).ToList()
                    };
                    model.AllowProcessing = model.ActiveRequestIsAnswered && (model.RequestStatus == RequestStatus.Accepted || model.RequestStatus == RequestStatus.AcceptedNewInterpreterAppointed) && (await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded;
                }
                model.EventLog = new EventLogModel
                {
                    Entries = EventLogHelper.GetEventLog(order, order.Requests.All(r => r.Status == RequestStatus.DeclinedByBroker || r.Status == RequestStatus.DeniedByTimeLimit)
                        ? order.Requests.OrderBy(r => r.RequestId).Last()
                        : null)
                            .OrderBy(e => e.Timestamp)
                            .ThenBy(e => e.Weight)
                            .ToList(),
                };
                if (request != null)
                {
                    model.ActiveRequest = RequestModel.GetModelFromRequest(request, true);
                    model.ActiveRequest.InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null;
                }
                else
                {
                    model.ActiveRequest = new RequestModel();
                }
                model.ActiveRequest.OrderModel = model;
                model.ActiveRequest.OrderModel.OrderRequirements = model.OrderRequirements;
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Replace(int replacingOrderId, string cancelMessage)
        {
            var order = GetOrder(replacingOrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Replace)).Succeeded)
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
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Replace(ReplaceOrderModel model)
        {
            if (ModelState.IsValid)
            {
                Order order = GetOrder(model.ReplacingOrderId.Value);
                if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Replace)).Succeeded)
                {
                    using (var trn = await _dbContext.Database.BeginTransactionAsync())
                    {
                        Order replacementOrder = new Order(order);
                        model.UpdateOrder(replacementOrder, true);
                        await _orderService.ReplaceOrder(order, replacementOrder, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                        await _dbContext.SaveChangesAsync();
                        trn.Commit();
                        return RedirectToAction("Index", "Home", new { message = "Ersättningsuppdrag är skickat" });
                    }
                }
            }
            return View(model);
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Create()
        {
            var now = _clock.SwedenNow.DateTime;
            var firstWorkDay = _dateCalculationService.GetFirstWorkDay(now).Date;
            var panicTime = _dateCalculationService.GetFirstWorkDay(firstWorkDay).Date;
            if (now.Hour >= 14)
            {
                //Add day if after 14...
                panicTime = _dateCalculationService.GetFirstWorkDay(panicTime.AddDays(1).Date).Date;
            }
            DateTime nextPanicTime = _dateCalculationService.GetFirstWorkDay(panicTime.AddDays(1).Date).Date;
            var user = await _userManager.GetUserAsync(User);
            var model = new OrderModel()
            {
                LastTimeForRequiringLatestAnswerBy = panicTime.ToString("yyyy-MM-dd"),
                NextLastTimeForRequiringLatestAnswerBy = nextPanicTime.ToString("yyyy-MM-dd"),
                CreatedBy = user.CompleteContactInformation
            };
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
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

                    await _orderService.CreateRequest(order, latestAnswerBy: model.LatestAnswerBy);

                    _dbContext.SaveChanges();
                    trn.Commit();
                    return RedirectToAction(nameof(Sent), new { id = order.OrderId });
                }
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public ActionResult Confirm(OrderModel model)
        {
            Order order = CreateNewOrder();
            model.UpdateOrder(order);
            var updatedModel = OrderModel.GetModelFromOrderForConfirmation(order);

            updatedModel.RegionName = _dbContext.Regions
                .Single(r => r.RegionId == model.RegionId).Name;

            updatedModel.LanguageName = order.OtherLanguage ?? _dbContext.Languages
            .Single(l => l.LanguageId == model.LanguageId).Name;
            updatedModel.LatestAnswerBy = model.LatestAnswerBy;

            //get pricelisttype for customer and get calculated price
            PriceListType pricelistType = _dbContext.CustomerOrganisations.Single(c => c.CustomerOrganisationId == order.CustomerOrganisation.CustomerOrganisationId).PriceListType;
            updatedModel.OrderCalculatedPriceInformationModel = new PriceInformationModel { Header = "Beräknat preliminärt pris", PriceInformationToDisplay = _orderService.GetOrderPriceinformationForConfirmation(order, pricelistType), UseDisplayHideInfo = true, Description = "Om inget krav eller önskemål om specifik kompetensnivå har angetts i bokningsförfrågan beräknas kostnaden enligt taxan för arvodesnivå Auktoriserad tolk. Slutlig arvodesnivå kan då avvika beroende på vilken tolk som tillsätts enligt principen för kompetensprioritering." };

            if (order.Attachments?.Count() > 0)
            {
                List<FileModel> attachments = new List<FileModel>();
                foreach (int attId in order.Attachments.Select(a => a.AttachmentId))
                {
                    Attachment a = _dbContext.Attachments.Single(f => f.AttachmentId == attId);
                    attachments.Add(new FileModel { FileName = a.FileName, Id = a.AttachmentId, Size = a.Blob.Length });
                }
                updatedModel.AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer",
                    DisplayFiles = attachments
                };
            }


            //check reasonable duration time for order (more than 10h or less than 1h)
            int minutes = (int)(order.EndAt - order.StartAt).TotalMinutes;
            updatedModel.WarningOrderTimeInfo = minutes > 600 ? "Observera att tiden för tolkuppdraget är längre än normalt, för att ändra tiden gå tillbaka till föregående steg genom att klicka på Ändra, om angiven tid är korrekt kan bokningen skickas som vanligt." : minutes < 60 ? "Observera att tiden för tolkuppdraget är kortare än normalt, för att ändra tiden gå tillbaka till föregående steg genom att klicka på Ändra, om angiven tid är korrekt kan bokningen skickas som vanligt." : string.Empty;

            //check if order is far in future (more than 2 years ahead)
            updatedModel.WarningOrderTimeInfo = string.IsNullOrEmpty(updatedModel.WarningOrderTimeInfo) ? order.StartAt.DateTime.AddYears(-2) > _clock.SwedenNow.DateTime ? "Observera att tiden för tolkuppdraget ligger långt fram i tiden, för att ändra tiden gå tillbaka till föregående steg genom att klicka på Ändra, om angiven tid är korrekt kan bokningen skickas som vanligt." : string.Empty : updatedModel.WarningOrderTimeInfo;

            var user = _userManager.Users.Where(u => u.Id == User.GetUserId()).Single();
            updatedModel.ContactPerson = order.ContactPersonId.HasValue ? _userManager.Users.Where(u => u.Id == order.ContactPersonId).Single().CompleteContactInformation : string.Empty;
            updatedModel.CreatedBy = user.CompleteContactInformation;
            updatedModel.CustomerName = user.CustomerOrganisation.Name;
            return PartialView("Confirm", updatedModel);
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Sent(int id)
        {
            Order order = GetOrder(id);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                var model = OrderModel.GetModelFromOrder(order);
                model.OrderCalculatedPriceInformationModel = GetPriceinformationToDisplay(order);
                model.OrderCalculatedPriceInformationModel.CenterHeader = true;
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Approve(ProcessRequestModel model)
        {
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(o => o.CustomerOrganisation)
                .Single(o => o.OrderId == model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded)
            {
                var request = order.Requests.Single(r => r.RequestId == model.RequestId);
                if (!request.CanApprove)
                {
                    _logger.LogWarning("Wrong status when trying to Approve request. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction(nameof(View), new { id = order.OrderId });
                }
                _orderService.ApproveRequestAnswer(request, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Cancel(CancelOrderModel model)
        {
            var order = _dbContext.Orders
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Single(o => o.OrderId == model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Cancel)).Succeeded)
            {
                if (order.ActiveRequest == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Beställningen kunde inte avbokas" });
                }
                if (model.AddReplacementOrder)
                {
                    //Forward the message
                    return RedirectToAction(nameof(Replace), new { replacingOrderId = model.OrderId, cancelMessage = model.CancelMessage });
                }
                _orderService.CancelOrder(order, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = model.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmCancellation(int requestId)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .SingleAsync(r => r.RequestId == requestId);

            if ((await _authorizationService.AuthorizeAsync(User, request.Order, Policies.View)).Succeeded)
            {
                if (request.Status != RequestStatus.CancelledByBroker)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det fanns ingen avbokning att bekräfta på denna bokning." });
                }
                await _orderService.ConfirmCancellationByBroker(request, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = "Avbokning är bekräftad" });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ConfirmNoAnswer(int orderId)
        {
            var order = await _dbContext.Orders.SingleAsync(o => o.OrderId == orderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
            {
                if (order.Status != OrderStatus.NoBrokerAcceptedOrder)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Denna bokning var inte avböjd av samtliga förmedlingar." });
                }
                await _orderService.ConfirmNoAnswer(order, User.GetUserId(), User.TryGetImpersonatorId());
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Deny(ProcessRequestModel model)
        {
            var order = await _dbContext.Orders.Include(o => o.Requests)
                .ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .SingleAsync(o => o.OrderId == model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Accept)).Succeeded)
            {
                var request = order.Requests.Single(r => r.RequestId == model.RequestId);
                if (!request.CanDeny)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Det går inte att neka denna tillsättningen" });
                }
                await _orderService.DenyRequestAnswer(request, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = order.OrderId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> ChangeContactPerson(OrderChangeContactPersonModel model)
        {
            var order = GetOrder(model.OrderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                if (model.ContactPersonId == order.ContactPersonId)
                {
                    return RedirectToAction(nameof(View), new { id = order.OrderId });
                }
                order.ChangeContactPerson(_clock.SwedenNow, User.GetUserId(), 
                    User.TryGetImpersonatorId(), _dbContext.Users.SingleOrDefault(u => u.Id == model.ContactPersonId));
                _notificationService.OrderContactPersonChanged(order);

                await _dbContext.SaveChangesAsync();

                if ((await _authorizationService.AuthorizeAsync(User, order, Policies.View)).Succeeded)
                {
                    return RedirectToAction(nameof(View), new { id = order.OrderId });
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { message = $"Person med rätt att granska rekvisition för bokning {order.OrderNumber} är ändrad" });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> UpdateExpiry(int orderId, DateTimeOffset latestAnswerBy)
        {
            var order = GetOrder(orderId);

            if ((await _authorizationService.AuthorizeAsync(User, order, Policies.Edit)).Succeeded)
            {
                var request = order.Requests.SingleOrDefault(r => r.Status == RequestStatus.AwaitingDeadlineFromCustomer);
                if (request == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Denna bokning behöver inte få sista svarstid satt." });
                }

                _orderService.SetRequestExpiryManually(request, latestAnswerBy);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { message = $"Sista svarstid för bokning {order.OrderNumber} är satt" });
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
                Header = "Beräknat pris enligt bokningsbekräftelse",
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
                Header = "Beräknat pris enligt ursprunglig bokningsförfrågan",
                UseDisplayHideInfo = true
            };
        }

        private Order CreateNewOrder()
        {
            var user = _dbContext.Users
               .Include(u => u.CustomerOrganisation)
               .Single(u => u.Id == User.GetUserId());
            var impersonator = User.TryGetImpersonatorId();
            AspNetUser impersonatingUser = null;
            if (impersonator.HasValue)
            {
                impersonatingUser = _dbContext.Users.Single(u => u.Id == impersonator);
            }
            return new Order(user, impersonatingUser, user.CustomerOrganisation, _clock.SwedenNow);
        }

        private Order GetOrder(int id)
        {
            return _dbContext.Orders
                .Include(o => o.ReplacedByOrder)
                .Include(o => o.ReplacingOrder)
                .Include(o => o.ReplacingOrder).ThenInclude(o => o.CreatedByUser)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Region)
                .Include(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.InterpreterLocations)
                .Include(o => o.CompetenceRequirements)
                .Include(o => o.OrderStatusConfirmations).ThenInclude(os => os.ConfirmedByUser)
                .Include(o => o.Attachments).ThenInclude(o => o.Attachment)
                .Include(o => o.OrderContactPersonHistory).ThenInclude(cph => cph.PreviousContactPersonUser)
                .Include(o => o.OrderContactPersonHistory).ThenInclude(cph => cph.ChangedByUser)
                .Include(o => o.Requirements).ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions).ThenInclude(r => r.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions).ThenInclude(r => r.ProcessedUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.AnsweringUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.AnswerDisputingUser)
                .Include(o => o.Requests).ThenInclude(r => r.Complaints).ThenInclude(c => c.TerminatingUser)
                .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.AnsweringUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReceivedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ProcessingUser)
                .Include(o => o.Requests).ThenInclude(r => r.CancelledByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(rr => rr.Ranking).ThenInclude(ra => ra.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(rr => rr.Requisitions).ThenInclude(u => u.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(rr => rr.Complaints).ThenInclude(u => u.CreatedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.ReplacingRequest).ThenInclude(r => r.Interpreter)
                .Include(o => o.Requests).ThenInclude(r => r.RequestStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                .Include(o => o.Requests).ThenInclude(r => r.Attachments).ThenInclude(a => a.Attachment)
                .Include(o => o.Requests).ThenInclude(r => r.Order)
                .Single(o => o.OrderId == id);
        }
    }
}
