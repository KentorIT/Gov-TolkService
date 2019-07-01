using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Linq.Dynamic.Core;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.ApplicationAdministrator)]
    public class EmailController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;

        public EmailController(
            TolkDbContext dbContext,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _clock = clock;
        }

        public ActionResult List(EmailFilterModel model)
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
                model.FilterMessage = "Om ingen filtrering görs så sätts datumsökningen till att söka på senaste dygnet för att minska antalaet sökträffar.";
            }

            return View(new EmailListModel
            {
                FilterModel = model,
                Items = model.Apply(_dbContext.OutboundEmails
                    .Select(e =>
                    new EmailListItemModel
                    {
                        CreatedAt = e.CreatedAt,
                        Subject = e.Subject,
                        Body = e.PlainBody,
                        Recipient = e.Recipient,
                        SentAt = e.DeliveredAt
                    }))
            });
        }

    }
}
