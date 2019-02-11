using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Microsoft.EntityFrameworkCore;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.Admin)]
    public class SystemMessageController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<SystemMessageController> _logger;

        public SystemMessageController(
            TolkDbContext dbContext,
            ILogger<SystemMessageController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public ActionResult List()
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
    }
}
