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

namespace Tolk.Web.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger _logger;

        public ComplaintController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            ILogger<RequisitionController> logger
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<IActionResult> View(int id)
        {
            var complaint = GetComplaint(id);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.View)).Succeeded)
            {
                return View(ComplaintViewModel.GetViewModelFromComplaint(complaint, User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId), User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId)));
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
                    CreateEmailOnComplaintAction(complaint);
                    var user = _dbContext.Users
                        .Include(u => u.CustomerOrganisation)
                        .Single(u => u.Id == complaint.CreatedBy);
                    return RedirectToAction(nameof(View), new { id = complaint.ComplaintId });
                }
                return Forbid();
            }
            return View("Create", model);
        }

        public IActionResult List(ComplaintFilterModel model)
        {
            var customerId = User.TryGetCustomerOrganisationId();
            var brokerId = User.TryGetBrokerId();
            var items = _dbContext.Complaints
                .Include(c => c.Request)
                    .ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                .Include(c => c.Request)
                    .ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Where(c => c.Request.Ranking.Broker.BrokerId == brokerId ||
                     c.Request.Order.CustomerOrganisationId == customerId);
            // Filters
            if (model == null)
            {
                model = new ComplaintFilterModel();
            }
            if (!User.IsInRole(Roles.SuperUser) && customerId.HasValue)
            {
                model.CustomerContactId = User.GetUserId();
            }
            if (model != null)
            {
                items = model.Apply(items);
            }
            model.IsCustomerSuperUser = User.IsInRole(Roles.SuperUser) && customerId.HasValue;
            model.IsBrokerUser = brokerId.HasValue;
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
            if (ModelState.IsValid)
            {
                var complaint = GetComplaint(complaintId);
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    complaint.Answer(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), null, ComplaintStatus.Confirmed);
                    _dbContext.SaveChanges();
                    return RedirectToAction(nameof(View), new { id = complaintId });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = complaintId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Dispute(DisputeComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                var complaint = GetComplaint(model.ComplaintId);
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    complaint.Answer(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DisputeMessage, ComplaintStatus.Disputed);
                    _dbContext.SaveChanges();
                    CreateEmailOnComplaintAction(complaint);
                    return RedirectToAction(nameof(View), new { id = model.ComplaintId });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.ComplaintId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AcceptDispute(AnswerDisputeComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                return await AnswerDispute(model, ComplaintStatus.TerminatedAsDisputeAccepted);
            }
            return RedirectToAction(nameof(View), new { id = model.ComplaintId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Refute(AnswerDisputeComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                return await AnswerDispute(model, ComplaintStatus.DisputePendingTrial);
            }
            return RedirectToAction(nameof(View), new { id = model.ComplaintId });
        }

        private async Task<IActionResult> AnswerDispute(AnswerDisputeComplaintModel model, ComplaintStatus status)
        {
            var complaint = GetComplaint(model.ComplaintId);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
            {
                complaint.AnswerDispute(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.AnswerDisputedMessage, status);
                _dbContext.SaveChanges();
                CreateEmailOnComplaintAction(complaint);
                return RedirectToAction(nameof(View), new { id = model.ComplaintId });
            }
            return Forbid();
        }


        private Request GetRequest(int id)
        {
            return _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
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
                .Include(r => r.Request).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
                .Single(o => o.ComplaintId == id);
        }

        private void CreateEmailOnComplaintAction(Complaint complaint)
        {
            string receipent;
            string subject;
            string body;
            string orderNumber = complaint.Request.Order.OrderNumber;
            switch (complaint.Status)
            {
                case ComplaintStatus.Created:
                    receipent = complaint.Request.Ranking.Broker.EmailAddress;
                    subject = $"En reklamation har registrerats på avrop {orderNumber}";
                    body = $"Reklamation för avrop {orderNumber} har skapats med följande meddelande:\n{complaint.ComplaintType.GetDescription()}\n{complaint.ComplaintMessage}";
                    break;
                case ComplaintStatus.Disputed:
                    receipent = complaint.CreatedByUser.Email;
                    subject = $"Reklamation kopplad till order {orderNumber} har blivit bestriden";
                    body = $"Reklamation för avrop {orderNumber} har bestridits med följande meddelande:\n{complaint.AnswerMessage}";
                    break;
                case ComplaintStatus.DisputePendingTrial:
                    receipent = complaint.Request.Ranking.Broker.EmailAddress;
                    subject = $"Ert bestridande av reklamation ogillades på avrop {orderNumber}";
                    body = $"Bestridande av reklamation för avrop {orderNumber} har ogillats med följande meddelande:\n{complaint.AnswerDisputedMessage}";
                    break;
                case ComplaintStatus.TerminatedAsDisputeAccepted:
                    receipent = complaint.Request.Ranking.Broker.EmailAddress;
                    subject = $"Ert bestridande av reklamation har godtagits på avrop {orderNumber}";
                    body = $"Bestridande av reklamation för avrop {orderNumber} har godtagits med följande meddelande:\n{complaint.AnswerDisputedMessage}";
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
                _logger.LogInformation($"No email sent for complaint action {complaint.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }
    }
}
