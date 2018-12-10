﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Helpers;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Tolk.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;

        public HomeController(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            ISwedishClock clock,
            IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _clock = clock;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index(string message)
        {
            if (!_dbContext.IsUserStoreInitialized)
            {
                return RedirectToAction("CreateInitialUser", "Account");
            }

            if (!User.Identity.IsAuthenticated)
            {
                return View("IndexNotLoggedIn");
            }
            if (!User.IsImpersonated())
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var hasPassword = await _userManager.HasPasswordAsync(user);

                    if (!hasPassword)
                    {
                        return RedirectToAction("RegisterNewAccount", "Account");
                    }
                    if (!(await _authorizationService.AuthorizeAsync(User, Policies.ViewMenuAndStartLists)).Succeeded)
                    {
                        return RedirectToAction("Edit", "Account");
                    }
                }
            }
            return View(new StartViewModel
            {
                PageTitle = User.IsInRole(Roles.Admin) ? "Startsida för tolkavropstjänsten" : "Aktiva bokningsförfrågningar",
                Message = message,
                ConfirmationMessages = await GetConfirmationMessages(),
                StartLists = await GetStartLists()
            });
        }

        private async Task<IEnumerable<StartViewModel.StartList>> GetStartLists()
        {
            var result = Enumerable.Empty<StartViewModel.StartList>();

            if ((await _authorizationService.AuthorizeAsync(User, Policies.Customer)).Succeeded)
            {
                result = result.Union(GetCustomerStartLists());
            }
            if ((await _authorizationService.AuthorizeAsync(User, Policies.Broker)).Succeeded)
            {
                result = result.Union(GetBrokerStartLists());
            }
            if ((await _authorizationService.AuthorizeAsync(User, Policies.Interpreter)).Succeeded)
            {
                result = result.Union(GetInterpreterStartLists());
            }

            return result;
        }

        private IEnumerable<StartViewModel.StartList> GetCustomerStartLists()
        {

            var actionList = new List<StartListItemModel>();

            //accepted orders to approve
            actionList.AddRange(_dbContext.Orders.Include(o => o.Requests)
                .Include(o => o.Language).Where(o => (o.Status == OrderStatus.RequestResponded || o.Status == OrderStatus.RequestRespondedNewInterpreter) && o.CreatedBy == User.GetUserId())
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.Any() ? o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime : _clock.SwedenNow.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderAcceptedForApproval, ButtonAction = "View", ButtonController = "Order" }).ToList());

            //Requisitions to review (for user and where user is contact person)
            actionList.AddRange(_dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Where(r => r.Status == RequisitionStatus.Created && r.Request.Order.Status == OrderStatus.Delivered &&
                (r.Request.Order.CreatedBy == User.GetUserId() || r.Request.Order.ContactPersonId == User.GetUserId()))
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt.DateTime, EndDateTime = r.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = r.Request.Order.OrderId, InfoDate = r.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = r.RequisitionId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitonArrived, ButtonAction = "Process", ButtonController = "Requisition" }).ToList()); ;

            //Disputed complaints
            actionList.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Disputed &&
                c.CreatedBy == User.GetUserId()).Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Select(c => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt.DateTime, EndDateTime = c.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = c.Request.Order.OrderId, InfoDate = c.AnsweredAt.HasValue ? c.AnsweredAt.Value.DateTime : c.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = c.ComplaintId, Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name, OrderNumber = c.Request.Order.OrderNumber, Status = StartListItemStatus.ComplaintEvent, ButtonAction = "View", ButtonController = "Complaint" }).ToList());

            //Non-answered-requests, is this correct with arrivaldate and check on orderstatus?
            actionList.AddRange(_dbContext.Orders.Include(o => o.Requests)
                .Include(o => o.Language).Where(o => o.Status == OrderStatus.NoBrokerAcceptedOrder && o.CreatedBy == User.GetUserId())
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().ExpiresAt.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderNotAnswered, ButtonAction = "View", ButtonController = "Order" }).ToList());

            //Cancelled by broker
            actionList.AddRange(_dbContext.Orders.Include(o => o.Requests)
                .Include(o => o.Language).Where(o => o.Status == OrderStatus.CancelledByBroker && o.CreatedBy == User.GetUserId())
                .Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CancelledAt.Value.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel?)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, ButtonItemId = o.OrderId, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderCancelled, ButtonAction = "View", ButtonController = "Order" }).ToList());

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av myndighet ({count} st)" : "Kräver handling av myndighet",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som kräver handling av myndigheten",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            //Sent orders
            var sentOrders = _dbContext.Orders
                .Include(o => o.Language).
                Where(o => o.Status == OrderStatus.Requested &&
            o.CreatedBy == User.GetUserId() && o.EndAt > _clock.SwedenNow).Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.CreatedAt.DateTime, InfoDateDescription = "Skickad: ", CompetenceLevel = CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.OtherLanguage ?? o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderCreated }).ToList();

            count = sentOrders.Any() ? sentOrders.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Skickade bokningar ({count} st)" : "Skickade bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är skickade",
                StartListObjects = sentOrders
            };

            //Approved orders 
            var approvedOrders = _dbContext.Orders.Include(o => o.Requests)
            .Include(o => o.Language).Where(o => o.Status == OrderStatus.ResponseAccepted &&
            o.CreatedBy == User.GetUserId() && o.EndAt > _clock.SwedenNow).Select(o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt.DateTime, EndDateTime = o.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().AnswerDate.Value.DateTime, CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.OrderApproved }).ToList();

            count = approvedOrders.Any() ? approvedOrders.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är tillsatta",
                StartListObjects = approvedOrders
            };

            // Awaiting requisition
            var awaitRequisition = _dbContext.Orders.Include(o => o.Requests)
            .Include(o => o.Language).Where(o => o.Status == OrderStatus.ResponseAccepted &&
            o.CreatedBy == User.GetUserId() && o.EndAt < _clock.SwedenNow && !(o.Requests.Any(r => r.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.AutomaticApprovalFromCancelledOrder || req.Status == RequisitionStatus.Created)))).Select
            (o => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = o.StartAt, EndDateTime = o.EndAt }, DefaulListAction = "View", DefaulListController = "Order", DefaultItemId = o.OrderId, InfoDate = o.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = o.Requests.Any() ? (CompetenceAndSpecialistLevel)o.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault().CompetenceLevel : CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = string.Empty, Language = o.Language.Name, OrderNumber = o.OrderNumber, Status = StartListItemStatus.RequisitionAwaited }).ToList();

            count = awaitRequisition.Any() ? awaitRequisition.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Inväntar rekvisition ({count} st)" : "Inväntar rekvisition",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som inväntar rekvisition",
                StartListObjects = awaitRequisition
            };
        }

        private IEnumerable<StartViewModel.StartList> GetBrokerStartLists()
        {
            var brokerId = User.GetBrokerId();
            var actionList = new List<StartListItemModel>();

            //requests with status received, created, denied, cancelled by customer
            actionList.AddRange(_dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.RequestStatusConfirmations)
                .Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received || r.Status == RequestStatus.CancelledByCreatorWhenApproved || r.Status == RequestStatus.DeniedByCreator) &&
                r.Ranking.BrokerId == brokerId && !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator))
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = GetInfoDateForBroker(r).Value, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = GetStartListStatusForBroker(r.Status), ButtonAction = r.Status == RequestStatus.Created || r.Status == RequestStatus.Received ? "Process" : "View", ButtonController = "Request", LatestDate = r.Status == RequestStatus.Created || r.Status == RequestStatus.Received ? (DateTime?)r.ExpiresAt.DateTime : null }).ToList());

            //Complaints
            actionList.AddRange(_dbContext.Complaints.Where(c => c.Status == ComplaintStatus.Created && c.Request.Ranking.BrokerId == brokerId)
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Select(c => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = c.Request.Order.StartAt.DateTime, EndDateTime = c.Request.Order.EndAt.DateTime }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = c.Request.RequestId, InfoDate = c.CreatedAt.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = c.Request.Order.CustomerOrganisation.Name, ButtonItemId = c.ComplaintId, Language = c.Request.Order.OtherLanguage ?? c.Request.Order.Language.Name, OrderNumber = c.Request.Order.OrderNumber, Status = StartListItemStatus.ComplaintEvent, ButtonAction = "View", ButtonController = "Complaint" }).ToList());

            //To be reported
            actionList.AddRange(_dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => r.Status == RequestStatus.Approved && r.Order.StartAt < _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                 .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.Order.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = StartListItemStatus.RequisitionToBeCreated, ButtonAction = "Create", ButtonController = "Requisition" }).ToList());

            //Denied requisitions
            actionList.AddRange(_dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.DeniedByCustomer &&
                !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) && r.Request.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.ProcessedAt.Value.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, ButtonItemId = r.RequisitionId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionDenied, ButtonAction = "View", ButtonController = "Requisition" }).ToList());

            //TODO? få veta om myndighet ej besvarat alls (det syns i och för sig)? 

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kräver handling av förmedling ({count} st)" : "Kräver handling av förmedling",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som kräver handling av förmedling",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            //approved orders and not approved but answered
            var answeredRequests = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => (r.Status == RequestStatus.Approved || r.Status == RequestStatus.Accepted) && r.Order.StartAt > _clock.SwedenNow && !r.Requisitions.Any() && r.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.AnswerDate.Value.DateTime, InfoDateDescription = "Tillsatt: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = StartListItemStatus.OrderApproved }).ToList();

            count = answeredRequests.Any() ? answeredRequests.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Tillsatta bokningar ({count} st)" : "Tillsatta bokningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar som är tillsatta",
                StartListObjects = answeredRequests
            };

            //sent requisitions
            var sentRequisitions = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.Created && r.Request.Ranking.BrokerId == brokerId)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Request", DefaultItemId = r.RequestId, InfoDate = r.Request.Order.EndAt.DateTime, InfoDateDescription = "Skickad: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionCreated }).ToList();

            count = sentRequisitions.Any() ? sentRequisitions.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Skickade rekvisitioner ({count} st)" : "Skickade rekvisitioner",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar med skickad rekvisition",
                StartListObjects = sentRequisitions
            };
        }

        private DateTime? GetInfoDateForBroker(Request r)
        {
            return r.Status == RequestStatus.CancelledByCreator ? r.CancelledAt?.DateTime : r.Status == RequestStatus.DeniedByCreator ? r.AnswerProcessedAt?.DateTime : r.CreatedAt.DateTime;
        }

        private StartListItemStatus GetStartListStatusForBroker(RequestStatus requestStatus)
        {
            return requestStatus == RequestStatus.Received ? StartListItemStatus.RequestReceived : requestStatus == RequestStatus.Created ? StartListItemStatus.RequestArrived : requestStatus == RequestStatus.DeniedByCreator ? StartListItemStatus.RequestDenied : StartListItemStatus.OrderCancelled;
        }

        private IEnumerable<StartViewModel.StartList> GetInterpreterStartLists()
        {
            var interpreterId = User.GetInterpreterId();
            var actionList = new List<StartListItemModel>();

            //To be reported
            actionList.AddRange(_dbContext.Requests
                .Where(r => r.Status == RequestStatus.Approved && r.Order.StartAt < _clock.SwedenNow && !r.Requisitions.Any() && r.InterpreterId == interpreterId)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                 .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Assignment", DefaultItemId = r.RequestId, InfoDate = r.Order.EndAt.DateTime, InfoDateDescription = "Utfört: ", CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, ButtonItemId = r.RequestId, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = StartListItemStatus.RequisitionToBeCreated, ButtonAction = "Create", ButtonController = "Requisition" }).ToList());

            //Denied requisitions
            actionList.AddRange(_dbContext.Requisitions
                .Where(r => !r.ReplacedByRequisitionId.HasValue && r.Status == RequisitionStatus.DeniedByCustomer &&
                !r.Request.Requisitions.Any(req => req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Created) && r.Request.InterpreterId == interpreterId)
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(req => req.Order).ThenInclude(o => o.CustomerOrganisation)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Request.Order.StartAt, EndDateTime = r.Request.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Assignment", DefaultItemId = r.RequestId, InfoDate = r.ProcessedAt.Value.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Request.Order.CustomerOrganisation.Name, ButtonItemId = r.RequisitionId, Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name, OrderNumber = r.Request.Order.OrderNumber, Status = StartListItemStatus.RequisitionDenied, ButtonAction = "View", ButtonController = "Requisition" }).ToList());

            var count = actionList.Any() ? actionList.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Lista med aktiva bokningsförfrågningar att hantera ({count} st)" : "Lista med aktiva bokningsförfrågningar att hantera",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga aktiva bokningsförfrågningar att hantera",
                StartListObjects = actionList,
                HasReviewAction = true
            };

            //kommande uppdrag
            var assignments = _dbContext.Requests.Where(r => (r.Status == RequestStatus.Approved) &&
                r.Order.StartAt > _clock.SwedenNow && !r.Requisitions.Any() && r.InterpreterId == interpreterId)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Select(r => new StartListItemModel { Orderdate = new TimeRange { StartDateTime = r.Order.StartAt, EndDateTime = r.Order.EndAt }, DefaulListAction = "View", DefaulListController = "Assignment", DefaultItemId = r.RequestId, InfoDate = r.AnswerProcessedAt.Value.DateTime, CompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter, CustomerName = r.Order.CustomerOrganisation.Name, Language = r.Order.OtherLanguage ?? r.Order.Language.Name, OrderNumber = r.Order.OrderNumber, Status = StartListItemStatus.OrderApproved }).ToList();

            count = assignments.Any() ? assignments.Count() : 0;

            yield return new StartViewModel.StartList
            {
                Header = count > 0 ? $"Kommande bokningsförfrågningar ({count} st)" : "Kommande bokningsförfrågningar",
                EmptyMessage = count > 0 ? string.Empty : "För tillfället finns det inga kommande bokningsförfrågningar",
                StartListObjects = assignments
            };
        }

        private async Task<IEnumerable<StartViewModel.ConfirmationMessage>> GetConfirmationMessages()
        {
            if ((await _authorizationService.AuthorizeAsync(User, Policies.Interpreter)).Succeeded)
            {
                return await _dbContext.InterpreterBrokers
                    .Where(ib => ib.InterpreterId == User.GetInterpreterId() && !ib.AcceptedByInterpreter)
                    .Select(ib => new StartViewModel.ConfirmationMessage
                    {
                        Header = "Förmedlingar som vill skicka uppdrag till dig som tolk",
                        BrokerName = ib.Broker.Name,
                        Message = "Förmedling som vill lägga till dig",
                        Controller = "Interpreter",
                        Action = "AcceptBroker",
                        Id = ib.BrokerId
                    }).ToListAsync();
            }

            return Enumerable.Empty<StartViewModel.ConfirmationMessage>();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policies.TimeTravel)]
        public IActionResult TimeTravel(DateTime date, TimeSpan time, string action)
        {
            var clock = (TimeTravelClock)_clock;

            switch (action)
            {
                case "Jump":
                    var targetDateTime = date.Add(time).ToDateTimeOffsetSweden();
                    clock.TimeTravelTicks = targetDateTime.ToUniversalTime().Ticks - DateTimeOffset.UtcNow.Ticks;
                    break;
                case "Reset":
                    clock.TimeTravelTicks = 0;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
