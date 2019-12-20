using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.AppOrSysAdmin)]
    public class EmailController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly INotificationService _notificationService;

        public EmailController(
            TolkDbContext dbContext,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
        }

        public IActionResult List()
        {
            return View(new EmailListModel { FilterModel = new EmailFilterModel() });
        }

        [HttpPost]
        public async Task<IActionResult> ListEmails(IDataTablesRequest request)
        {
            var model = new EmailFilterModel();
            await TryUpdateModelAsync(model);

            var emails = _dbContext.OutboundEmails.Select(e => e);
            return AjaxDataTableHelper.GetData(request, emails.Count(), model.Apply(emails), x => x.Select(e =>
                    new EmailListItemModel
                    {
                        OutboundEmailId = e.OutboundEmailId,
                        CreatedAt = e.CreatedAt,
                        Subject = e.Subject,
                        Body = e.PlainBody,
                        Recipient = e.Recipient,
                        SentAt = e.DeliveredAt,
                        ResentAt = e.ReplacedByEmail.CreatedAt
                    }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<EmailListItemModel>());
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
                oldEmail.OutboundEmailId,
                User.GetUserId()
            );

            return RedirectToAction(nameof(List));
        }

    }
}
