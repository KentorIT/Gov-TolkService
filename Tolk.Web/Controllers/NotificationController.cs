using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.ApplicationAdminOrBrokerCA)]
    public class NotificationController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ListNotificationService _listNotificationService;
        public NotificationController(TolkDbContext dbContext, ListNotificationService listNotificationService)
        {
            _dbContext = dbContext;
            _listNotificationService = listNotificationService;
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
                try
                {
                    foreach (var notificationType in model.SelectedTypes)
                    {
                        try
                        {
                            await _listNotificationService.Archive(model.BrokerId, model.ArchiveToDate, User.GetUserId(), User.TryGetImpersonatorId(), notificationType);
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            // handle, logg and continue
                        }
                    }
                    return RedirectToAction("Index", "Home", new { message = "meddelanden har blivit arkiverade" });

                }
                catch (Exception ex)
                {
                    //TODO: Log, and send a much better message to home page!
                    return RedirectToAction("Index", "Home", new { errorMessage = ex.Message });
                }
            }
            //Todo: anta att det är fel med något som de kan se meddelande om på sidan
            return RedirectToAction(nameof(Index), new { errorMessage = "All data var inte korrekt ifylld"});
        }

    }
}
