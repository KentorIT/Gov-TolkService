using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.AppOrSysAdmin)]
    public class EmailController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;

        public EmailController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
        }

        public IActionResult List(EmailFilterModel model)
        {

            if (!model.HasActiveFilters)
            {
                if (model.DateCreated != null)
                {
                    return RedirectToAction(nameof(List));
                }

                model.DateCreated = new DateRange
                {
                    Start = _clock.SwedenNow.Date.AddDays(-1),
                    End = _clock.SwedenNow.Date
                };
                model.FilterMessage = "Om ingen filtrering görs så sätts datumsökningen till att söka på senaste dygnet för att minska antalet sökträffar.";
            }
            return View(new EmailListModel
            {
                FilterModel = model,
                Items = model.Apply(_dbContext.OutboundEmails
                .Select(e =>
                    new EmailListItemModel
                    {
                        OutboundEmailId = e.OutboundEmailId,
                        CreatedAt = e.CreatedAt,
                        Subject = e.Subject,
                        Body = e.PlainBody,
                        Recipient = e.Recipient,
                        SentAt = e.DeliveredAt,
                        ResentAt = _dbContext.OutboundEmails.Where(oe => oe.ReplacingEmailId.HasValue && oe.ReplacingEmailId == e.OutboundEmailId).SingleOrDefault().CreatedAt
                    }))
            });
        }

        public IActionResult View(int id)
        {
            return View(EmailModel.GetModelFromOutboundEmail(_dbContext.OutboundEmails
                .Include(e => e.ReplacedByEmail).Single(e => e.OutboundEmailId == id), User.IsInRole(Roles.ApplicationAdministrator)));
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Resend(int id)
        {

            var oldEmail = _dbContext.OutboundEmails.Include(e => e.ReplacedByEmail)
                .SingleOrDefault(e => e.OutboundEmailId == id);
            if (oldEmail == null || oldEmail.ReplacedByEmail != null)
            {
                return View(nameof(View), EmailModel.GetModelFromOutboundEmail(oldEmail, User.IsInRole(Roles.ApplicationAdministrator), "Det gick inte att skicka om detta e-postmeddelande"));
            }

            _notificationService.CreateReplacingEmail(
                oldEmail.Recipient,
                oldEmail.Subject,
                oldEmail.PlainBody,
                oldEmail.HtmlBody,
                oldEmail.OutboundEmailId
            );

            return RedirectToAction(nameof(List));
        }

    }
}
