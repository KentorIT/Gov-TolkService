using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Microsoft.EntityFrameworkCore;
using Tolk.Web.Models;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.AppOrSysAdmin)]
    public class FaqController : Controller
    {

        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;

        public FaqController(
            TolkDbContext dbContext,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _clock = clock;
        }

        public IActionResult List(FaqFilterModel model)
        {
            if (model == null)
            {
                model = new FaqFilterModel();
            }
            var faqs = _dbContext.Faq.Include(f => f.FaqDisplayUserRoles).Select(f => f);

            faqs = model.Apply(faqs);
            return View(new FaqListModel
            {
                FilterModel = model,
                Items = faqs.Select(f => new FaqListItemModel
                {
                    DisplayedFor = f.FaqDisplayUserRoles.Select(fd => fd.DisplayUserRole),
                    Question = f.Question,
                    Answer = f.Answer,
                    IsDisplayed = f.IsDisplayed
                })
            });
        }
    }
}