using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.ApplicationAdminOrBrokerCA)]
    public class WebhookController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;

        public WebhookController(TolkDbContext dbContext,
            INotificationService notificationService,
            IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
        }

        public IActionResult List()
        {
            return View(new WebHookListModel
            {
                FilterModel = new WebHookFilterModel
                {
                    IsAppAdministrator = User.IsInRole(Roles.ApplicationAdministrator)
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ListWebhooks(IDataTablesRequest request)
        {
            var model = new WebHookFilterModel();
            await TryUpdateModelAsync(model);

            var webhooks = _dbContext.OutboundWebHookCalls.Select(e => e);
            if (!User.IsInRole(Roles.ApplicationAdministrator))
            {
                webhooks = webhooks.Where(w => w.RecipientUser.BrokerId == User.TryGetBrokerId());
            }
            return AjaxDataTableHelper.GetData(request, webhooks.Count(), model.Apply(webhooks), x => x.Select(wh => new WebHookListItemModel
            {
                CreatedAt = wh.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                DeliveredAt = wh.DeliveredAt != null ? wh.DeliveredAt.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                FailedTries = wh.FailedTries,
                HasBeenResent = wh.ResentHookId != null ? "Ja" : "Nej",
                NotificationType = wh.NotificationType.GetDescription(),
                OutboundWebHookCallId = wh.OutboundWebHookCallId,
                BrokerName = wh.RecipientUser.Broker.Name,
                ListColor = (
                         (wh.FailedTries >= 5 && wh.ResentHookId == null) ? "red-border-left" :
                         (wh.FailedTries < 5 && wh.DeliveredAt == null) ? "yellow-border-left" :
                         (wh.DeliveredAt != null) ? "green-border-left" :
                         "gray-border-left")
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<WebHookListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(WebHookListItemModel.BrokerName)).Visible = User.IsInRole(Roles.ApplicationAdministrator);
            return Json(definition);
        }

        public async Task<IActionResult> View(int id)
        {
            var notification = await _dbContext.OutboundWebHookCalls
                .Include(wh => wh.FailedCalls)
                .Include(wh => wh.RecipientUser).ThenInclude(u => u.Broker)
                .Include(wh => wh.ReplacingWebHook)
                .SingleOrDefaultAsync(wh => wh.OutboundWebHookCallId == id);

            if (notification != null && (await _authorizationService.AuthorizeAsync(User, notification, Policies.View)).Succeeded)
            {
                return View(new WebHookModel
                {
                    OutboundWebHookCallId = notification.OutboundWebHookCallId,
                    CreatedAt = notification.CreatedAt,
                    DeliveredAt = notification.DeliveredAt,
                    BrokerName = notification.RecipientUser.Broker.Name,
                    Payload = notification.Payload,
                    NotificationType = notification.NotificationType,
                    RecipientUrl = notification.RecipientUrl,
                    ReplacedBy = notification.ResentHookId,
                    Replaces = notification.ReplacingWebHook?.OutboundWebHookCallId,
                    FailedTries = notification.FailedCalls.Select(f => new FailedTryModel { FailedAt = f.FailedAt.DateTime, ErrorMessage = f.ErrorMessage }).ToList(),
                    AllowResend = notification.FailedCalls.Count() >= 5 && User.TryGetBrokerId() != null && !notification.ResentHookId.HasValue,
                    ShowBroker = User.IsInRole(Roles.ApplicationAdministrator)
                });
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(int webhookId)
        {
            var notification = await _dbContext.OutboundWebHookCalls
                .Include(wh => wh.RecipientUser)
                .SingleOrDefaultAsync(wh => wh.OutboundWebHookCallId == webhookId);
            if (notification != null && (await _authorizationService.AuthorizeAsync(User, notification, Policies.Replace)).Succeeded)
            {
                _notificationService.ResendWebHook(notification);
                return RedirectToAction("List");
            }
            return Forbid();

        }
    }
}
