using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Authorization;
using Microsoft.EntityFrameworkCore;
using Tolk.Web.Models;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Helpers;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.Admin)]
    public class SystemMessageController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<SystemMessageController> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISwedishClock _clock;

        public SystemMessageController(
            TolkDbContext dbContext,
            ILogger<SystemMessageController> logger,
            IAuthorizationService authorizationService,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _logger = logger;
            _authorizationService = authorizationService;
            _clock = clock;
        }

        public IActionResult List()
        {
            var systemMessages = _dbContext.SystemMessages
                .Include(s => s.CreatedByUser)
                .Include(s => s.LastUpdatedByUser).ToList();

            return View(new SystemMessageListModel
            {
                Items =
                systemMessages.
                Select(s => new SystemMessageListItemModel
                {
                    CreatedLastUpdatedAt = s.LastUpdatedAt ?? s.CreatedAt,
                    CreatedLastUpdatedBy = s.LastUpdatedByUser?.FullName ?? s.CreatedByUser.FullName,
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

        public ActionResult Edit(int id)
        {
            var systemMessage = _dbContext.SystemMessages
                .Single(s => s.SystemMessageId == id);

            return View(new SystemMessageModel
            {
                SystemMessageId = systemMessage.SystemMessageId,
                SystemMessageText = systemMessage.SystemMessageText,
                SystemMessageHeader = systemMessage.SystemMessageHeader,
                DisplayedForUserTypeGroup = systemMessage.SystemMessageUserTypeGroup,
                SystemMessageType = new RadioButtonGroup { SelectedItem = SelectListService.SystemMessageTypes.Single(e => e.Value == systemMessage.SystemMessageType.ToString()) },
                SystemMessageTypeCheckedIndex = SelectListService.SystemMessageTypes.ToList().IndexOf(SelectListService.SystemMessageTypes.Single(e => e.Value == systemMessage.SystemMessageType.ToString())).ToString(),
                DisplayDate = new RequiredDateRange { Start = systemMessage.ActiveFrom.DateTime, End = systemMessage.ActiveTo.DateTime }
            });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(SystemMessageModel model)
        {
            if (ModelState.IsValid)
            {
                var sysMessage = _dbContext.SystemMessages
                .Single(s => s.SystemMessageId == model.SystemMessageId);
                sysMessage.Update(_clock.SwedenNow, User.GetUserId(), model.DisplayDate.Start.ToDateTimeOffsetSweden(), model.DisplayDate.End.ToDateTimeOffsetSweden(), model.SystemMessageHeader, model.SystemMessageText, EnumHelper.Parse<SystemMessageType>(model.SystemMessageType.SelectedItem.Value), model.DisplayedForUserTypeGroup);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("List");
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(SystemMessageModel model)
        {
            if (ModelState.IsValid)
            {
                var sysMessage = new SystemMessage();
                sysMessage.Create(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DisplayDate.Start.ToDateTimeOffsetSweden(), model.DisplayDate.End.ToDateTimeOffsetSweden(), model.SystemMessageHeader, model.SystemMessageText, EnumHelper.Parse<SystemMessageType>(model.SystemMessageType.SelectedItem.Value), model.DisplayedForUserTypeGroup);
                await _dbContext.AddAsync(sysMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("List");
            }
            return View(model);
        }
    }
}
