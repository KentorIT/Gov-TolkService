using DataTables.AspNet.Core;
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
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize]
    public class ComplaintController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger _logger;
        private readonly ComplaintService _complaintService;
        private readonly EventLogService _eventLogService;

        public ComplaintController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            ILogger<ComplaintController> logger,
            ComplaintService complaintService,
            EventLogService eventLogService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _logger = logger;
            _complaintService = complaintService;
            _eventLogService = eventLogService;
        }

        public IActionResult List()
        {
            return View(new ComplaintListModel
            {
                FilterModel = new ComplaintFilterModel
                {
                    CustomerUnits = User.TryGetAllCustomerUnits(),
                    IsBroker = User.TryGetBrokerId().HasValue,
                    IsAdmin = User.IsInRole(Roles.SystemAdministrator) || User.IsInRole(Roles.ApplicationAdministrator),
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ListComplaints(IDataTablesRequest request)
        {
            var model = new ComplaintFilterModel();
            await TryUpdateModelAsync(model);
            model.IsCustomerCentralAdminOrOrderHandler = User.IsInRole(Roles.CentralAdministrator) || User.IsInRole(Roles.CentralOrderHandler);
            var brokerId = User.TryGetBrokerId();
            var customerOrganisationId = User.TryGetCustomerOrganisationId();
            model.IsBroker = brokerId.HasValue;
            if (model.IsBroker)
            {
                model.BrokerId = brokerId;
            }
            else
            {
                model.CustomerOrganisationId = customerOrganisationId;
                model.UserId = User.GetUserId();
                model.CustomerUnits = User.TryGetAllCustomerUnits();
            }
            IQueryable<Complaint> complaints = customerOrganisationId.HasValue ?
                 model.GetComplaintsFromOrders(_dbContext.Orders.Select(o => o)) : brokerId.HasValue ?
                  complaints = model.GetComplaintsFromRequests(_dbContext.Requests.Select(o => o)) : null;
            if (complaints == null)
            {
                return Forbid();
            }
            return AjaxDataTableHelper.GetData(request, complaints.Count(), model.Apply(complaints), x => x.Select(c => new ComplaintListItemModel
            {
                OrderRequestId = customerOrganisationId.HasValue ? c.Request.OrderId : c.RequestId,
                BrokerName = c.Request.Ranking.Broker.Name,
                CustomerName = c.Request.Order.CustomerOrganisation.Name,
                ComplaintType = c.ComplaintType,
                CreatedAt = c.CreatedAt.ToSwedishString("yyyy-MM-dd HH:mm"),
                OrderNumber = c.Request.Order.OrderNumber,
                RegionName = c.Request.Order.Region.Name,
                Status = c.Status,
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<ComplaintListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(ComplaintListItemModel.CustomerName)).Visible = User.TryGetBrokerId().HasValue; //or is sys/app admin 
            definition.Single(d => d.Name == nameof(ComplaintListItemModel.BrokerName)).Visible = User.TryGetCustomerOrganisationId().HasValue; //or is sys/app admin 
            return Json(definition);
        }

        public async Task<IActionResult> View(int id)
        {
            var complaint = await _dbContext.Complaints.GetFullComplaintById(id);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.View)).Succeeded)
            {
                var isCustomer = User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId);
                ComplaintViewModel model = ComplaintViewModel.GetViewModelFromComplaint(complaint, $"Complaint/{nameof(GetEventLog)}/{id}");
                model.IsBroker = User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId);
                model.IsCustomer = isCustomer;
                model.IsAdmin = User.IsInRole(Roles.SystemAdministrator);
                model.AllowAnwserOnDispute = !model.IsAdmin &&
                    complaint.Status == ComplaintStatus.Disputed &&
                    isCustomer &&
                    (await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded;
                return PartialView(model);
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Create(int id)
        {
            Request request = await _dbContext.Requests.GetRequestForOtherViewsById(id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
            {
                if (!request.IsApprovedOrDelivered)
                {
                    _logger.LogWarning("Wrong status when trying to Create complaint. Status: {request.Status}, RequestId {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction("Index", "Home", new { errormessage = "Bokning har inte rätt status för att kunna göra en reklamation" });
                }
                return View(ComplaintModel.GetModelFromRequest(request));
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Create(ComplaintModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetSimpleRequestById(model.RequestId);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
                {
                    try
                    {
                        await _complaintService.Create(request, User.GetUserId(), User.TryGetImpersonatorId(), model.Message, model.ComplaintType.Value);
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning("Wrong status or complaint exists when trying to Create complaint. Status: {request.Status}, RequestId {request.RequestId}", request.Status, request.RequestId);
                        return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                    }
                    return RedirectToAction("View", "Order", new { id = request.OrderId, tab = "complaint" });
                }
                return Forbid();
            }
            return View(nameof(Create), model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Broker)]
        public async Task<IActionResult> Accept(int complaintId)
        {
            var complaint = await _dbContext.Complaints.GetFullComplaintById(complaintId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    if (!complaint.CanAnswer)
                    {
                        _logger.LogWarning("Wrong status when trying to Accept complaint. Status: {complaint.Status}, ComplaintId: {complaint.ComplaintId}", complaint.Status, complaint.ComplaintId);
                        return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                    }

                    _complaintService.Accept(complaint, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Broker)]
        public async Task<IActionResult> Dispute(DisputeComplaintModel model)
        {
            var complaint = await _dbContext.Complaints.GetFullComplaintById(model.ComplaintId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    if (!complaint.CanAnswer)
                    {
                        _logger.LogWarning("Wrong status when trying to Dispute complaint. Status: {complaint.Status}, ComplaintId: {complaint.ComplaintId}", complaint.Status, complaint.ComplaintId);
                        return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                    }

                    _complaintService.Dispute(complaint, User.GetUserId(), User.TryGetImpersonatorId(), model.DisputeMessage);
                    await _dbContext.SaveChangesAsync();

                    return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> AcceptDispute(AnswerDisputeComplaintModel model)
        {
            var complaint = await _dbContext.Complaints.GetFullComplaintById(model.ComplaintId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    if (!complaint.CanAnswerDispute)
                    {
                        _logger.LogWarning("Wrong status when trying to Accept Dispute complaint. Status: {complaint.Status}, ComplaintId: {complaint.ComplaintId}", complaint.Status, complaint.ComplaintId);
                        return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                    }

                    _complaintService.AcceptDispute(complaint, User.GetUserId(), User.TryGetImpersonatorId(), model.AnswerDisputedMessage);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Refute(AnswerDisputeComplaintModel model)
        {
            var complaint = await _dbContext.Complaints.GetFullComplaintById(model.ComplaintId);
            if (ModelState.IsValid)
            {
                if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded)
                {
                    if (!complaint.CanAnswerDispute)
                    {
                        _logger.LogWarning("Wrong status when trying to Refute complaint. Status: {complaint.Status}, ComplaintId: {complaint.ComplaintId}", complaint.Status, complaint.ComplaintId);

                        return RedirectToAction("View", "Request", new { id = complaint.RequestId, tab = "complaint" });
                    }

                    _complaintService.Refute(complaint, User.GetUserId(), User.TryGetImpersonatorId(), model.RefuteMessage);
                    await _dbContext.SaveChangesAsync();

                    return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
                }
                return Forbid();
            }
            return RedirectToAction("View", "Order", new { id = complaint.Request.OrderId, tab = "complaint" });
        }

        [HttpPost]
        public async Task<IActionResult> GetEventLog(int id)
        {
            var complaint = await _dbContext.Complaints.GetFullComplaintById(id);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = _eventLogService.GetEventLogForComplaint(complaint, complaint.Request.Order.CustomerOrganisation.Name, complaint.Request.Ranking.Broker.Name)
                        .OrderBy(e => e.Timestamp)
                });
            }
            return Forbid();
        }
    }
}
