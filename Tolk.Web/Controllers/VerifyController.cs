using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize]
    public class VerifyController : Controller
    {
        private readonly VerificationService _verificationService;

        public VerifyController(VerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpGet]
        public async Task<JsonResult> Interpreter(string id, int orderId, CompetenceAndSpecialistLevel competenceLevel)
        {
            var result = await _verificationService.VerifyInterpreter(id, orderId, competenceLevel);
            var model = new VerificationResultModel
            {
                Value = result,
                Description = EnumHelper.GetDescription(result),
            };
            return Json(model);
        }
    }
}