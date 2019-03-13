using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.SuperUser)]
    [Authorize(Policy = Policies.Broker)]
    public class WebhookController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationService _notificationService;

        public WebhookController(TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            NotificationService notificationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _notificationService = notificationService;
        }
        public IActionResult List(WebHookFilterModel model)
        {
            if (model == null)
            {
                model = new WebHookFilterModel();
            }

            if (!User.IsInRole(Roles.SuperUser))
            {
                return Forbid();
            }

            int? brokerId = User.TryGetBrokerId();
            int? apiUser = _dbContext.Users.Where(u => u.BrokerId == brokerId && u.IsApiUser && u.IsActive)
                .SingleOrDefault()?.Id;

            IQueryable<OutboundWebHookCall> items = _dbContext.OutboundWebHookCalls
                .Where(wh => wh.RecipientUserId == apiUser);

            items = model.Apply(items);

            return View(
                new WebHookListModel
                {
                    Items = items.Select(wh => new WebHookListItemModel
                    {
                        CreatedAt = wh.CreatedAt.DateTime,
                        DeliveredAt = wh.DeliveredAt == null ? null : (DateTime?)wh.DeliveredAt.Value.DateTime,
                        FailedTries = wh.FailedTries,
                        HasBeenResent = wh.ResentHookId != null,
                        NotificationType = wh.NotificationType,
                        OutboundWebHookCallId = wh.OutboundWebHookCallId,
                        ListColor = "gray-border-bottom " + (
                         (wh.FailedTries >= 5 && wh.ResentHookId == null) ? "red-border-left" :
                         (wh.FailedTries < 5 && wh.DeliveredAt == null) ? "yellow-border-left" :
                         (wh.DeliveredAt != null) ? "green-border-left" :
                         "gray-border-left")
                    }).OrderByDescending(wh => wh.CreatedAt),
                    FilterModel = model
                });
        }

        public IActionResult View(int id)
        {
            if (!User.IsInRole(Roles.SuperUser))
            {
                return Forbid();
            }
            int? brokerId = User.TryGetBrokerId();
            int? apiUser = _dbContext.Users.Where(u => u.BrokerId == brokerId && u.IsApiUser && u.IsActive)
                .SingleOrDefault()?.Id;

            var notification = _dbContext.OutboundWebHookCalls
                .Where(wh => wh.RecipientUserId == apiUser)
                .Include(wh => wh.FailedCalls)
                .Select(wh => new WebHookModel
                {
                    OutboundWebHookCallId = wh.OutboundWebHookCallId,
                    CreatedAt = wh.CreatedAt,
                    DeliveredAt = wh.DeliveredAt,
                    Payload = wh.Payload,
                    NotificationType = wh.NotificationType,
                    RecipientUrl = wh.RecipientUrl,
                    ReplacedBy = wh.ResentHookId,
                    Replaces = _dbContext.OutboundWebHookCalls.Where(w => w.ResentHookId == wh.OutboundWebHookCallId && w.ResentHookId != null).Select(w => (int?)w.OutboundWebHookCallId).SingleOrDefault(),
                    FailedTries = wh.FailedCalls.Select(f => new FailedTryModel { FailedAt = f.FailedAt.DateTime, ErrorMessage = f.ErrorMessage }).ToList()

                })
                .SingleOrDefault(wh => wh.OutboundWebHookCallId == id);

            if (notification == null)
            {
                return Forbid();
            }

            return View(notification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Resend(int WebHookId)
        {
            if (!User.IsInRole(Roles.SuperUser))
            {
                return Forbid();
            }

            int? brokerId = User.TryGetBrokerId();
            int? apiUser = _dbContext.Users.Where(u => u.BrokerId == brokerId && u.IsApiUser && u.IsActive)
                .SingleOrDefault()?.Id;

            var oldNotification = _dbContext.OutboundWebHookCalls.Where(wh => wh.RecipientUserId == apiUser).SingleOrDefault(wh => wh.OutboundWebHookCallId == WebHookId);
            if (oldNotification == null)
            {
                return RedirectToAction("View", new { id = WebHookId });
            }

            _notificationService.ResendWebHook(oldNotification);


            return RedirectToAction("List");
        }
    }
}
