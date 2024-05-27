using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.ApplicationAdministrator)]
    public class PeppolController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly TolkOptions _options;

        public PeppolController(TolkDbContext dbContext,
            INotificationService notificationService,
            IAuthorizationService authorizationService,             
            IOptions<TolkOptions> options)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
            _options = options?.Value;            
        }

        public IActionResult List()
        {
            return View(new PeppolListModel
            {
                FilterModel = new PeppolFilterModel()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListPeppolMessages(IDataTablesRequest request)
        {
            var model = new PeppolFilterModel();
            await TryUpdateModelAsync(model);

            var peppolMessages = _dbContext.OutboundPeppolMessages.Select(e => e);
            return AjaxDataTableHelper.GetData(request, peppolMessages.Count(), model.Apply(peppolMessages), x => x.Select(p => new PeppolListItemModel
            {
                CreatedAt = p.CreatedAt.ToSwedishString("yyyy-MM-dd HH:mm"),
                DeliveredAt = p.DeliveredAt != null ? p.DeliveredAt.Value.ToSwedishString("yyyy-MM-dd HH:mm") : "-",
                FailedTries = p.FailedTries,
                HasBeenResent = p.ManualResendSetAt != null ? "Ja" : "Nej",
                NotificationType = p.NotificationType.GetDescription(),
                OutboundPeppolMessageId = p.OutboundPeppolMessageId,
                CustomerName = p.PeppolMessagePayload.Request.Order.CustomerOrganisation.Name,
                ListColor = (
                         (p.FailedTries >= _options.Peppol.MaxUploadRetries && p.DeliveredAt == null) ? "red-border-left" :
                         (p.FailedTries < _options.Peppol.MaxUploadRetries && p.DeliveredAt == null) ? "yellow-border-left" :
                         (p.DeliveredAt != null) ? "green-border-left" :
                         "gray-border-left")
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<PeppolListItemModel>().ToList();
            return Json(definition);
        }

        public async Task<IActionResult> View(int id)
        {
            var message = await _dbContext.OutboundPeppolMessages.GetPeppolMessageById(id);
            message.FailedCalls = await _dbContext.FailedPeppolMessages.GetFailedPeppolMessagesByPeppolMessageId(id).ToListAsync();

            if (message != null && (await _authorizationService.AuthorizeAsync(User, message, Policies.View)).Succeeded)
            {
                return View(new PeppolModel
                {
                    OutboundPeppolMessageId = message.OutboundPeppolMessageId,
                    CreatedAt = message.CreatedAt,
                    DeliveredAt = message.DeliveredAt,
                    CustomerName = message.PeppolMessagePayload.Request.Order.CustomerOrganisation.Name,
                    NotificationType = message.NotificationType,
                    ReplacedBy = message.ReplacedByMessage?.OutboundPeppolMessageId,
                    Replaces = message.ReplacingPeppolMessageId,
                    FailedTries = message.FailedCalls.Select(f => new FailedTryModel { FailedAt = f.FailedAt.DateTime, ErrorMessage = f.ErrorMessage }).ToList(),
                    AllowResend = message.FailedCalls.Count >= _options.Peppol.MaxUploadRetries && message.ManualResendSetAt == null,
                });
            }
            return Forbid();
        }
        [HttpGet]
        public async Task<ActionResult> GetPayload(int id)
        {
            var payload = await _dbContext.OutboundPeppolMessages.GetPeppolMessageById(id);
            if (payload != null && (await _authorizationService.AuthorizeAsync(User, payload, Policies.View)).Succeeded)
            {                                
                return File(_options.Peppol.UseEnvelope ? payload.Payload : payload.PeppolMessagePayload.Payload, System.Net.Mime.MediaTypeNames.Application.Octet, $"PeppolMessage-{payload.PeppolMessagePayload.IdentificationNumber}.xml");
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(int peppolMessageId)
        {
            var notification = await _dbContext.OutboundPeppolMessages.GetPeppolMessageById(peppolMessageId);
            if (notification != null && (await _authorizationService.AuthorizeAsync(User, notification, Policies.Replace)).Succeeded)
            {             
                _notificationService.ResendPeppolMessage(notification, User.GetUserId(), User.TryGetImpersonatorId());
                return RedirectToAction("List");
            }
            return Forbid();
        }
    }
}
