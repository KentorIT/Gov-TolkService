using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    [Authorize]
    public class ComplaintController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger _logger;
        private readonly ComplaintService _complaintService;

        public ComplaintController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            ILogger<ComplaintController> logger,
            ComplaintService complaintService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _logger = logger;
            _complaintService = complaintService;
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

        public async Task<IActionResult> View(int id, bool returnPartial = false)
        {
            var complaint = GetComplaint(id);
            if ((await _authorizationService.AuthorizeAsync(User, complaint, Policies.View)).Succeeded)
            {
                var isCustomer = User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId);
                ComplaintViewModel model = ComplaintViewModel.GetViewModelFromComplaint(complaint);
                model.IsBroker = User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId);
                model.IsCustomer = isCustomer;
                model.IsAdmin = User.IsInRole(Roles.SystemAdministrator);
                model.AllowAnwserOnDispute = !model.IsAdmin &&
                    complaint.Status == ComplaintStatus.Disputed &&
                    isCustomer &&
                    (await _authorizationService.AuthorizeAsync(User, complaint, Policies.Accept)).Succeeded;
                if (returnPartial) { return PartialView(model); }
                return View(model);
            }
            return Forbid();
        }

        [Authorize(Policy = Policies.Customer)]
        public async Task<IActionResult> Create(int id)
        {
            Request request = GetRequest(id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
            {
                if (!request.CanCreateComplaint)
                {
                    _logger.LogWarning("Wrong status when trying to Create complaint. Status: {request.Status}, RequestId {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction("View", "Order", new { id = request.OrderId, tab = "complaint" });
                }
                //Get request model from db
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
                var request = await _dbContext.Requests
                    .Include(r => r.Order)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Complaints)
                    .SingleAsync(o => o.RequestId == model.RequestId);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateComplaint)).Succeeded)
                {
                    if (!request.CanCreateComplaint)
                    {
                        _logger.LogWarning("Wrong status when trying to Create complaint. Status: {request.Status}, RequestId {request.RequestId}", request.Status, request.RequestId);
                        return RedirectToAction("View", "Order", new { id = request.OrderId, tab = "complaint" });
                    }
                    _complaintService.Create(request, User.GetUserId(), User.TryGetImpersonatorId(), model.Message, model.ComplaintType.Value);
                    await _dbContext.SaveChangesAsync();
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
            var complaint = GetComplaint(complaintId);
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
            var complaint = GetComplaint(model.ComplaintId);
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
            var complaint = GetComplaint(model.ComplaintId);
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
            var complaint = GetComplaint(model.ComplaintId);
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

        private Request GetRequest(int id)
        {
            return _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Include(r => r.Complaints)
                .Single(o => o.RequestId == id);
        }

        private Complaint GetComplaint(int id)
        {
            return _dbContext.Complaints
                .Include(r => r.CreatedByUser)
                .Include(r => r.AnsweringUser).ThenInclude(u => u.Broker)
                .Include(r => r.AnswerDisputingUser)
                .Include(r => r.TerminatingUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
                .Single(o => o.ComplaintId == id);
        }

    }
}
