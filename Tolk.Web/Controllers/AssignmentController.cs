using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
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
        public async Task<IActionResult> ListAssignments(IDataTablesRequest request)
        {
#warning move include
            var model = new AssignmentFilterModel();
            await TryUpdateModelAsync(model);
            var requests = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
            .Where(r => r.Interpreter.InterpreterId == User.GetInterpreterId() &&
                (r.Status == RequestStatus.Approved ||
                r.Status == RequestStatus.CancelledByBroker ||
                r.Status == RequestStatus.CancelledByCreator ||
                r.Status == RequestStatus.CancelledByCreatorWhenApproved));
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.

            return AjaxDataTableHelper.GetData(request, requests.Count(), model.Apply(requests, _clock), x => x.Select(r =>
           new RequestListItemModel
           {
               RequestId = r.RequestId,
               LanguageName = r.Order.OtherLanguage ?? r.Order.Language.Name,
               OrderNumber = r.Order.OrderNumber,
               CustomerName = r.Order.CustomerOrganisation.Name,
               RegionName = r.Order.Region.Name,
               ExpiresAt = r.ExpiresAt,
               StartAt = r.Order.StartAt,
               EndAt = r.Order.EndAt,
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
#warning include-fest
            var request = _dbContext.Requests
                    .Include(r => r.PriceRows)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder)
                    .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(oa => oa.Attachment)
                    .Include(r => r.Attachments).ThenInclude(ra => ra.Attachment)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Where(r => r.RequestId == id).Single();
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return View(AssignmentModel.GetModelFromRequest(request, _clock.SwedenNow));
            }
            return Forbid();
        }
    }
}
