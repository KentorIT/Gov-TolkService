using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.ApplicationAdministrator)]
    public class NotificationController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ListNotificationService _listNotificationService;
        private readonly ILogger _logger;
        public NotificationController(TolkDbContext dbContext, ListNotificationService listNotificationService, ILogger<NotificationController> logger)
        {
            _dbContext = dbContext;
            _listNotificationService = listNotificationService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            bool isAppAdmin = User.IsInRole(Roles.ApplicationAdministrator);
            return View(new ArchivableNotificationsModel
            {
                IsApplicationAdministrator = isAppAdmin,
                BrokerId = !isAppAdmin ? User.TryGetBrokerId() : null
            });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult List(int id)
        {
            return PartialView("_ListArchivableNotifications", _listNotificationService.GetAllArchivableNotificationsForBroker(id));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Archive(ArchiveNotificationsModel model)
        {
            if (User.IsInRole(Roles.CentralAdministrator))
            {
                model.BrokerId = User.GetBrokerId();
            }
            if (ModelState.IsValid)
            {
                if (model.SelectedTypes == null)
                {
                    return RedirectToAction("Index", "Home", new { errorMessage = "Minst en notifieringstyp måste väljas" });
                }
                try
                {
                    bool hadErrors = false;
                    foreach (var notificationType in model.SelectedTypes)
                    {
                        try
                        {
                            await _listNotificationService.Archive(model.BrokerId, model.ArchiveToDate, User.GetUserId(), User.TryGetImpersonatorId(), notificationType);
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to archive home page messages of type {NotificationType} for {BrokerId}: {error}", notificationType, model.BrokerId, ex.Message);
                            hadErrors = true;
                        }
                    }
                    return RedirectToAction("Index", "Home", new { message = $"Meddelanden har blivit arkiverade. {(hadErrors ? "Notera: Alla typer arkiverades inte, se mer info i loggen": string.Empty)}" });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to archive home page messages for {BrokerId}: {error}", model.BrokerId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errorMessage = "Något gick fel vid arkiveringen av startlistenotifieringar!" });
                }
            }
            //Todo: anta att det är fel med något som de kan se meddelande om på sidan
            return RedirectToAction(nameof(Index), new { errorMessage = "All data var inte korrekt ifylld" });
        }

    }
}
