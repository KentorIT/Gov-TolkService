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
