﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{

    [Authorize]
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

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public IActionResult List(FaqFilterModel model)
        {
            model ??= new FaqFilterModel();
            var faqDisplayUserRolesWithFaqs = _dbContext.FaqDisplayUserRole.GetAllFaqWithFaqDisplayUserRoles().Select(f => f);
            var faqs = faqDisplayUserRolesWithFaqs.Select(f => f.Faq).Distinct();

            faqs = model.Apply(faqs);
            return View(new FaqListModel
            {
                FilterModel = model,
                Items = faqs.Select(f => new FaqListItemModel
                {
                    FaqId = f.FaqId,
                    CreatedAt = f.CreatedAt,
                    DisplayedFor = f.FaqDisplayUserRoles.Select(fd => fd.DisplayUserRole),
                    Question = f.Question,
                    Answer = f.Answer,
                    IsDisplayed = f.IsDisplayed
                })
            });
        }

        public IActionResult Faqs()
        {
            var faqIds = _dbContext.FaqDisplayUserRole.GetFaqDisplayUserRolesByDisplayUserRoles(DisplayUserRoleForCurrentUser)
                .Select(f => f.FaqId).Distinct();

            return View(new FaqListModel
            {
                IsBroker = User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId),
                Items = _dbContext.Faq.GetPublishedFaqWithFaqIds(faqIds)
               .Select(f => new FaqListItemModel
               {
                   Question = f.Question,
                   Answer = f.Answer
               })
            });
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public ActionResult Create()
        {
            return View(new FaqModel { IsDisplayed = true });
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
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

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public async Task <IActionResult> Edit(int id)
        {
            var faqDisplayUserRolesWithFaq = await _dbContext.FaqDisplayUserRole.GetFaqWithFaqDisplayUserRolesByFaqId(id).ToListAsync();
            var faq = faqDisplayUserRolesWithFaq.Select(f => f.Faq).First();

            return View(new FaqModel
            {
                FaqId = faq.FaqId,
                Answer = faq.Answer,
                Question = faq.Question,
                IsDisplayed = faq.IsDisplayed,
                DisplayForRoles = new CheckboxGroup
                {
                    SelectedItems = SelectListService.DisplayForUserRoles
                        .Where(item => faqDisplayUserRolesWithFaq
                            .Select(fd => fd.DisplayUserRole)
                            .Contains(EnumHelper.Parse<DisplayUserRole>(item.Value))
                        )
                },
            });
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
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
                var faqDisplayUserRolesWithFaq = await _dbContext.FaqDisplayUserRole.GetFaqWithFaqDisplayUserRolesByFaqId(model.FaqId).ToListAsync();
                faq = faqDisplayUserRolesWithFaq.Select(f => f.Faq).First();
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

        private IEnumerable<DisplayUserRole> DisplayUserRoleForCurrentUser
        {
            get
            {
                var isBroker = User.HasClaim(c => c.Type == TolkClaimTypes.BrokerId);

                if (isBroker || User.HasClaim(c => c.Type == TolkClaimTypes.CustomerOrganisationId))
                {
                    yield return isBroker ? DisplayUserRole.BrokerUsers : DisplayUserRole.CustomerUsers;
                }
                if (User.IsInRole(Roles.CentralAdministrator))
                {
                    yield return isBroker ? DisplayUserRole.BrokerUserAdministrators : DisplayUserRole.CustomerUsersAdministrators;
                }
            }
        }

    }
}