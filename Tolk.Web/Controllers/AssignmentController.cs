using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Interpreter)]
    public class AssignmentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISwedishClock _clock;

        public AssignmentController(TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _clock = clock;
        }

        public IActionResult List()
        {
            return View(new AssignmentListModel { FilterModel = new AssignmentFilterModel() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListAssignments(IDataTablesRequest request)
        {
            var model = new AssignmentFilterModel();
            await TryUpdateModelAsync(model);
            var requests = _dbContext.Requests.GetRequestsForInterpreter(User.GetInterpreterId());

            return AjaxDataTableHelper.GetData(request, requests.Count(), model.Apply(requests, _clock), x => x.Select(r =>
            new RequestListItemModel
            {
                RequestId = r.RequestId,
                LanguageName = r.Order.OtherLanguage ?? r.Order.Language.Name,
                OrderNumber = r.Order.OrderNumber,
                CustomerName = r.Order.CustomerOrganisation.Name,
                RegionName = r.Order.Region.Name,
                ExpiresAt = r.ExpiresAt,
                StartAt = r.RespondedStartAt ?? r.Order.StartAt,
                EndAt = r.RespondedStartAt != null ? r.RespondedStartAt.Value.Add(r.Order.ExpectedLength.Value) : r.Order.EndAt,
                Status = r.Status
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<RequestListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(RequestListItemModel.ExpiresAtDisplay)).Visible = false;
            return Json(definition);
        }

        public async Task<IActionResult> View(int id)
        {
            var request = await _dbContext.Requests.GetRequestForInterpretertById(id);
            request.Requisitions = await _dbContext.Requisitions.GetRequisitionsForRequest(request.RequestId).ToListAsync();
            request.Order.InterpreterLocations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(request.OrderId).ToListAsync();
            request.PriceRows = await _dbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync();
            var model = AssignmentModel.GetModelFromRequest(request, _clock.SwedenNow);
            model.RequestAttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequest(request.RequestId, request.RequestGroupId), "Bifogade filer från förmedling");
            model.OrderAttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForOrderAndGroup(request.OrderId, request.Order.OrderGroupId), "Bifogade filer från myndighet");
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return View(AssignmentModel.GetModelFromRequest(request, _clock.SwedenNow));
            }
            return Forbid();
        }
    }
}
