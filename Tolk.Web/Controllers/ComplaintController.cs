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
            var complaint = _dbContext.Complaints
              .Single(o => o.ComplaintId == id);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.View)).Succeeded)
            {
                //var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
                return View();
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
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
            {
                //Get request model from db
                return View(ComplaintModel.GetModelFromRequest(request));
            }
            return Forbid();
        }

        public IActionResult List(ComplaintFilterModel model)
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(ComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                .Single(o => o.RequestId == model.RequestId);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
                {
                    var complaint = new Complaint
                    {
                        RequestId = model.RequestId,
                        ComplaintMessage = model.Message,
                        Status = ComplaintStatus.Created,
                        CreatedAt = _clock.SwedenNow,
                        CreatedBy = User.GetUserId(),
                        ImpersonatingCreatedBy = User.TryGetImpersonatorId()
                    };
                    request.CreateComplaint(complaint);
                    _dbContext.SaveChanges();
                    CreateEmailOnComplaintAction(complaint);
                    return RedirectToAction(nameof(View), new { id = complaint.ComplaintId });
                }
                return Forbid();
            }
            return View("Create", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            if (ModelState.IsValid)
            {
                var complaint = _dbContext.Complaints
                    .Single(r => r.ComplaintId == id);
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    complaint.Accept(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                    return RedirectToAction(nameof(View), new { id });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Dispute(DenyMessageDialogModel model)
        {
            if (ModelState.IsValid)
            {
                var complaint = _dbContext.Complaints
                    .Single(r => r.ComplaintId == model.ParentId);
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    complaint.Dispute(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.Message);
                    _dbContext.SaveChanges();
                    CreateEmailOnComplaintAction(complaint);
                    return RedirectToAction(nameof(View), new { id = model.ParentId});
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Accept), new { id = model.ParentId });
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
                    subject =  $"En reklamation har registrerats på avrop {orderNumber}";
                    body = $"Reklamation för avrop {orderNumber} har skapats med följande meddelande:\n{complaint.ComplaintType.GetDescription()}\n{complaint.ComplaintMessage}";
                    break;
                //case RequisitionStatus.Approved:
                //    receipent = complaint.CreatedByUser.Email;
                //    subject = body = $"Rekvisition för avrop {orderNumber} har godkänts";
                //    body = $"Rekvisition för avrop {orderNumber} har underkänts med följande meddelande:\n{complaint.DenyMessage}";
                //    break;
                //case RequisitionStatus.DeniedByCustomer:
                //    receipent = complaint.CreatedByUser.Email;
                //    subject = $"Rekvisition för avrop {orderNumber} har underkänts";
                //    body = $"Rekvisition för avrop {orderNumber} har underkänts med följande meddelande:\n{complaint.DenyMessage}";
                //    break;
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
