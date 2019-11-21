using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataTables.AspNet.Core;
using System.Linq;
using System;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{

    [Authorize(Policy = Policies.Broker)]
    [Authorize(Roles = Roles.CentralAdministrator)]
    public class InterpreterController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly InterpreterService _interpreterService;
        private readonly ISwedishClock _clock;

        public InterpreterController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            InterpreterService interpreterService,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _interpreterService = interpreterService;
            _clock = clock;
        }

        public IActionResult List(InterpreterListModel model = null)
        {
            return model.Message == null ? View(new InterpreterListModel { FilterModel = new InterpreterFilterModel() }) : View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ListInterpreters(IDataTablesRequest request)
        {
            var model = new InterpreterFilterModel();
            await TryUpdateModelAsync(model);

            IQueryable<InterpreterBroker> interpreters = _dbContext.InterpreterBrokers
            .Where(r => r.BrokerId == User.GetBrokerId());

            return AjaxDataTableHelper.GetData(request, interpreters.Count(), model.Apply(interpreters), x => x.Select(i => new InterpreterListItemModel
            {
                Id = i.InterpreterBrokerId,
                Email = i.Email,
                Name = i.FullName,
                OfficialInterpreterId = i.OfficialInterpreterId,
                IsActive = i.IsActive
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<InterpreterListItemModel>());
        }

        public async Task<IActionResult> View(int id)
        {
            var interpreter = _dbContext.InterpreterBrokers.SingleOrDefault(u => u.InterpreterBrokerId == id);
            if ((await _authorizationService.AuthorizeAsync(User, interpreter, Policies.View)).Succeeded)
            {
                return View(InterpreterModel.GetModelFromInterpreter(interpreter));
            }
            return Forbid();
        }

        public async Task<ActionResult> Edit(int id)
        {
            var interpreter = _dbContext.InterpreterBrokers.SingleOrDefault(u => u.InterpreterBrokerId == id);
            if ((await _authorizationService.AuthorizeAsync(User, interpreter, Policies.Edit)).Succeeded)
            {
                return View(InterpreterModel.GetModelFromInterpreter(interpreter));
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(InterpreterModel model)
        {
            if (ModelState.IsValid)
            {
                var interpreter = _dbContext.InterpreterBrokers.SingleOrDefault(u => u.InterpreterBrokerId == model.Id);
                if ((await _authorizationService.AuthorizeAsync(User, interpreter, Policies.Edit)).Succeeded)
                {
                    if (!_interpreterService.IsUniqueOfficialInterpreterId(model.OfficialInterpreterId, User.GetBrokerId(), model.Id))
                    {
                        ModelState.AddModelError(nameof(model.OfficialInterpreterId), $"Er förmedling har redan registrerat en tolk med detta tolknummer (Kammarkollegiets) i tjänsten.");
                    }
                    else
                    {
                        if (interpreter.IsActive != model.IsActive)
                        {
                            model.UpdateAndChangeStatusInterpreter(interpreter, interpreter.IsActive ? (int?)User.GetUserId() : null, interpreter.IsActive ? User.TryGetImpersonatorId() : null, interpreter.IsActive ? (DateTimeOffset?)_clock.SwedenNow : null);
                        }
                        else
                        {
                            model.UpdateInterpreter(interpreter);
                        }
                        await _dbContext.SaveChangesAsync();
                        return RedirectToAction(nameof(List), new InterpreterListModel { Message = "Tolkinformation har sparats" });
                    }
                }
            }
            return View(model);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(InterpreterModel model)
        {
            if (ModelState.IsValid)
            {
                if (!_interpreterService.IsUniqueOfficialInterpreterId(model.OfficialInterpreterId, User.GetBrokerId()))
                {
                    ModelState.AddModelError(nameof(model.OfficialInterpreterId), $"Er förmedling har redan registrerat en tolk med detta tolknummer (Kammarkollegiets) i tjänsten.");
                }
                else
                {
                    InterpreterBroker interpreter = new InterpreterBroker(User.GetBrokerId());
                    model.UpdateInterpreter(interpreter);
                    await _dbContext.AddAsync(interpreter);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(List), new InterpreterListModel { Message = "Ny tolk har skapats" });
                }
            }
            return View(model);
        }

    }
}
