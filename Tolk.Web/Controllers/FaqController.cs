using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
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
using Tolk.Web.Services;


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
                    FaqId = f.FaqId,
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
                return await CreateUpdateFaq(model, false);
            }
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var faq = _dbContext.Faq.Include(f => f.FaqDisplayUserRoles)
                .Single(f => f.FaqId == id);

            return View(new FaqModel
            {
                FaqId = faq.FaqId,
                Answer = faq.Answer,
                Question = faq.Question,
                IsDisplayed = faq.IsDisplayed,
                DisplayForRoles = new CheckboxGroup
                {
                    SelectedItems = SelectListService.DisplayForUserRoles
                        .Where(item => faq.FaqDisplayUserRoles
                            .Select(fd => fd.DisplayUserRole)
                            .Contains(EnumHelper.Parse<DisplayUserRole>(item.Value))
                        )
                },
            });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(FaqModel model)
        {
            if (ModelState.IsValid)
            {
                return await CreateUpdateFaq(model, true);
            }
            return View(model);
        }

        private async Task<IActionResult> CreateUpdateFaq(FaqModel model, bool update)
        {
            Faq faq = new Faq();
            var displayForRoles = model.DisplayForRoles.SelectedItems.Select(r => EnumHelper.Parse<DisplayUserRole>(r.Value));
            if (update)
            {
                faq = _dbContext.Faq.Include(f => f.FaqDisplayUserRoles)
                    .Single(s => s.FaqId == model.FaqId);
                faq.Update(_clock.SwedenNow, User.GetUserId(), model.IsDisplayed, model.Question, model.Answer, displayForRoles);
            }
            else
            {
                faq.Create(_clock.SwedenNow, User.GetUserId(), model.IsDisplayed, model.Question, model.Answer, displayForRoles);
                await _dbContext.AddAsync(faq);
            }
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("List");
        }

    }
}