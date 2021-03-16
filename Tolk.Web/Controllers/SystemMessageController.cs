using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.AppOrSysAdmin)]
    public class SystemMessageController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;

        public SystemMessageController(
            TolkDbContext dbContext,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _clock = clock;
        }

        public async Task<IActionResult> List()
        {
            var systemMessages = await _dbContext.SystemMessages.GetAllSystemMessages().ToListAsync();
            return View(new SystemMessageListModel
            {
                Items = systemMessages
                .Select(s => new SystemMessageListItemModel
                {
                    LastUpdatedCreatedAt = s.LastUpdatedCreatedAt,
                    LastUpdatedCreatedBy = s.LastUpdatedByUser?.FullName ?? s.CreatedByUser.FullName,
                    ActiveFrom = s.ActiveFrom,
                    ActiveTo = s.ActiveTo,
                    DisplayedFor = s.SystemMessageUserTypeGroup,
                    SystemMessageType = s.SystemMessageType,
                    SystemMessageHeader = s.SystemMessageHeader,
                    SystemMessageId = s.SystemMessageId
                })
            });
        }

        public ActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> Edit(int id)
        {
            var systemMessage = await _dbContext.SystemMessages.GetSystemMessageById(id);
            return View(SystemMessageModel.GetModelFromSystemMessage(systemMessage));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(SystemMessageModel model)
        {
            if (ModelState.IsValid)
            {
                return await CreateUpdateSystemMessage(model, true);
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(SystemMessageModel model)
        {
            if (ModelState.IsValid)
            {
                return await CreateUpdateSystemMessage(model, false);
            }
            return View(model);
        }

        private async Task<IActionResult> CreateUpdateSystemMessage(SystemMessageModel model, bool update)
        {
            if (model.DisplayDate.Start > model.DisplayDate.End)
            {
                model.SystemMessageTypeValue = EnumHelper.Parse<SystemMessageType>(model.SystemMessageType.SelectedItem.Value);
                ModelState.AddModelError($"{nameof(model.DisplayDate)}.{nameof(model.DisplayDate.Start)}", "Visningsdatum för nyheten är fel. Från och med datum kan inte vara större än till och med datum.");
            }
            else
            {
                if (update)
                {
                    var sysMessage = await _dbContext.SystemMessages.GetSystemMessageById(model.SystemMessageId);
                    sysMessage.Update(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DisplayDate.Start.ToDateTimeOffsetSweden(), model.DisplayDate.End.ToDateTimeOffsetSweden(), model.SystemMessageHeader, model.SystemMessageText, EnumHelper.Parse<SystemMessageType>(model.SystemMessageType.SelectedItem.Value), model.DisplayedForUserTypeGroup);
                }
                else
                {
                    var sysMessage = new SystemMessage();
                    sysMessage.Create(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DisplayDate.Start.ToDateTimeOffsetSweden(), model.DisplayDate.End.ToDateTimeOffsetSweden(), model.SystemMessageHeader, model.SystemMessageText, EnumHelper.Parse<SystemMessageType>(model.SystemMessageType.SelectedItem.Value), model.DisplayedForUserTypeGroup);
                    await _dbContext.AddAsync(sysMessage);
                }
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("List");
            }
            return View(model);
        }

    }
}
