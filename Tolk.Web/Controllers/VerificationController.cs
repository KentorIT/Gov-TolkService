using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VerificationController : Controller
    {
        private readonly VerificationService _verificationService;

        public VerificationController(VerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpGet]
        [Route("VerifyInterpreter")]
        public JsonResult VerifyInterpreter(string id, int languageId, CompetenceAndSpecialistLevel competenceLevel)
        {
            var result = _verificationService.VerifyInterpreter(id, languageId, competenceLevel);
            var model = new VerificationResultModel
            {
                Value = result,
                Description = EnumHelper.GetDescription(result),
            };
            return Json(model);
        }
    }
}