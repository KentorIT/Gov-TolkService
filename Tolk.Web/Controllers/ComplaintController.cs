using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
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
using System.Collections.Generic;

namespace Tolk.Web.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger _logger;
        private readonly NotificationService _notificationService;

        public ComplaintController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            ILogger<ComplaintController> logger,
            NotificationService notificationService
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _authorizationService = authorizationService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> View(int id, bool returnPartial = false)
        {
            var complaint = GetComplaint(id);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.View)).Succeeded)
            {
                ComplaintViewModel model = ComplaintViewModel.GetViewModelFromComplaint(complaint, User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId), User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId));
                if (returnPartial) { return PartialView(model); }
                return View(model);
            }
            return Forbid();
        }

        /// <summary>
        /// Create a complaint
        /// </summary>
        /// <param name="id">The Request to connect the complaint to</param>
        /// <returns></returns>
        public async Task<IActionResult> Create(int id)
        {
            Request request = GetRequest(id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
            {
                //Get request model from db
                return View(ComplaintModel.GetModelFromRequest(request));
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(ComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                .Include(r => r.Order)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Complaints)
                .Single(o => o.RequestId == model.RequestId);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
                {
                    var complaint = new Complaint
                    {
                        RequestId = model.RequestId,
                        ComplaintType = model.ComplaintType.Value,
                        ComplaintMessage = model.Message,
                        Status = ComplaintStatus.Created,
                        CreatedAt = _clock.SwedenNow,
                        CreatedBy = User.GetUserId(),
                        ImpersonatingCreatedBy = User.TryGetImpersonatorId()
                    };
                    request.CreateComplaint(complaint);
                    _dbContext.SaveChanges();
                    _notificationService.ComplaintCreated(complaint);
                    var user = _dbContext.Users
                        .Include(u => u.CustomerOrganisation)
                        .Single(u => u.Id == complaint.CreatedBy);
                    return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
                }
                return Forbid();
            }
            return View("Create", model);
        }

        public IActionResult List(ComplaintFilterModel model)
        {
            if (model == null)
            {
                model = new ComplaintFilterModel();
            }
            var customerId = User.TryGetCustomerOrganisationId();
            var brokerId = User.TryGetBrokerId();
            var userId = User.GetUserId();
            model.IsCustomerSuperUser = User.IsInRole(Roles.SuperUser) && customerId.HasValue;
            model.IsBrokerUser = brokerId.HasValue;
           
            var items = _dbContext.Complaints
                .Include(c => c.Request)
                    .ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Where(c => c.Request.Ranking.Broker.BrokerId == brokerId ||
                     c.Request.Order.CustomerOrganisationId == customerId);

            if (customerId.HasValue && !model.IsCustomerSuperUser)
            {
                items = items.Where(c => c.Request.Order.CreatedBy == userId || c.Request.Order.ContactPersonId == userId);
            }

            items = model.Apply(items);
        
            return View(
                new ComplaintListModel
                {
                    Items = items.Select(c => new ComplaintListItemModel
                    {
                        ComplaintId = c.ComplaintId,
                        BrokerName = c.Request.Ranking.Broker.Name,
                        CustomerName = c.Request.Order.CustomerOrganisation.Name,
                        ComplaintType = c.ComplaintType,
                        CreatedAt = c.CreatedAt,
                        OrderNumber = c.Request.Order.OrderNumber,
                        RegionName = c.Request.Order.Region.Name,
                        Status = c.Status,
                        Action = nameof(View)
                    }),
                    FilterModel = model
                });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Accept(int complaintId)
        {
            var complaint = GetComplaint(complaintId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    complaint.Answer(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), null, ComplaintStatus.Confirmed);
                    _dbContext.SaveChanges();
                    return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Dispute(DisputeComplaintModel model)
        {
            var complaint = GetComplaint(model.ComplaintId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    complaint.Answer(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DisputeMessage, ComplaintStatus.Disputed);
                    _dbContext.SaveChanges();
                    _notificationService.ComplaintDisputed(complaint);
                    return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AcceptDispute(AnswerDisputeComplaintModel model)
        {
            var complaint = GetComplaint(model.ComplaintId);
            if (ModelState.IsValid)
            {
                return await AnswerDispute(model, ComplaintStatus.TerminatedAsDisputeAccepted);
            }
            return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Refute(AnswerDisputeComplaintModel model)
        {
            var complaint = GetComplaint(model.ComplaintId);
            if (ModelState.IsValid)
            {
                return await AnswerDispute(model, ComplaintStatus.DisputePendingTrial);
            }
            return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
        }

        private async Task<IActionResult> AnswerDispute(AnswerDisputeComplaintModel model, ComplaintStatus status)
        {
            var complaint = GetComplaint(model.ComplaintId);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
            {
                complaint.AnswerDispute(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.AnswerDisputedMessage, status);
                _dbContext.SaveChanges();
                switch (status)
                {
                    case ComplaintStatus.DisputePendingTrial:
                        _notificationService.ComplaintDisputePendingTrial(complaint);
                        break;
                    case ComplaintStatus.TerminatedAsDisputeAccepted:
                        _notificationService.ComplaintTerminatedAsDisputeAccepted(complaint);
                        break;
                    default:
                        throw new NotImplementedException($"Notification for the complain status {status.GetDescription()} does not exist.");

                }
                return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
            }
            return Forbid();
        }

        private Request GetRequest(int id)
        {
            return _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Single(o => o.RequestId == id);
        }

        private Complaint GetComplaint(int id)
        {
            return _dbContext.Complaints
                .Include(r => r.CreatedByUser)
                .Include(r => r.AnsweringUser)
                .Include(r => r.AnswerDisputingUser)
                .Include(r => r.TerminatingUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
                .Single(o => o.ComplaintId == id);
        }

    }
}
