using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class LanguageController : Controller
    {

        private readonly TolkDbContext _dbContext;
        private readonly VerificationService _verificationService;

        public LanguageController(
            TolkDbContext dbContext,
            VerificationService verificationService)
        {
            _dbContext = dbContext;
            _verificationService = verificationService;
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> Verify()
        {
            return View(await _verificationService.ValidateTellusLanguageList());
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> UpdateCompetences()
        {
            return View(await _verificationService.UpdateTellusLanguagesCompetenceInfo());
        }

        [Authorize(Policy = Policies.ApplicationAdminOrBrokerCA)]
        public ActionResult List()
        {
            return View(
                _dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new LanguageListItem
                {
                    ISO_639_Code = l.ISO_639_Code,
                    Name = l.Name,
                    TellusName = l.TellusName,
                    HasLegal = l.HasLegal,
                    HasHealthcare = l.HasHealthcare,
                    HasAuthorized = l.HasAuthorized,
                    HasEducated = l.HasEducated
                }));
        }
    }
}