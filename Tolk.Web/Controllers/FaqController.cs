using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Enums;
using Microsoft.EntityFrameworkCore;
using Tolk.Web.Models;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Helpers;
using Tolk.Web.Authorization;


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

        public ActionResult Create()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(FaqModel model)
        {
            if (ModelState.IsValid)
            {
                var faq = new Faq();
                var displayForRoles = model.DisplayForRoles.SelectedItems.Select(r => EnumHelper.Parse<DisplayUserRole>(r.Value));

                faq.Create(_clock.SwedenNow, User.GetUserId(), model.IsDisplayed, model.Question, model.Answer, displayForRoles);
                await _dbContext.AddAsync(faq);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("List");
            }
            return View(model);
        }

    }
}