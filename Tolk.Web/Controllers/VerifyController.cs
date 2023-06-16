using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Broker)]
    public class VerifyController : Controller
    {
        private readonly VerificationService _verificationService;
        private readonly ILogger _logger;
        private readonly TolkDbContext _dbContext;

        public VerifyController(VerificationService verificationService, ILogger<OrderController> logger, TolkDbContext dbContext)
        {
            _verificationService = verificationService;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<JsonResult> InterpreterByInternalId(int id, CompetenceAndSpecialistLevel competenceLevel, int? orderId, int? orderGroupId)
        {
            int? brokerId = User.TryGetBrokerId();
            VerificationResult result = VerificationResult.NotFound;

            if (brokerId.HasValue)
            {
                var interpreter = await _dbContext.InterpreterBrokers.SingleOrDefaultAsync(i => i.BrokerId == brokerId && i.InterpreterBrokerId == id);
                if (interpreter != null)
                {
                    return await InterpreterByOfficialId(interpreter.OfficialInterpreterId, competenceLevel, orderId, orderGroupId);
                }
            }
            return WrapResultInJson(result);
        }

        [HttpGet]
        public async Task<JsonResult> InterpreterByOfficialId(string officialInterpreterId, CompetenceAndSpecialistLevel competenceLevel, int? orderId, int? orderGroupId)
        {
            if (!orderId.HasValue && orderGroupId.HasValue)
            {
                var order = await _dbContext.Orders.FirstOrDefaultAsync(g => g.OrderGroupId == orderGroupId);
                orderId = order?.OrderId;
            }
            if (!orderId.HasValue)
            {
                return WrapResultInJson(VerificationResult.NotFound);
            }
            _logger.LogInformation($"Verifying interpreterId {officialInterpreterId?.ToLoggableFormat()} for competence {competenceLevel} on order {orderId}");
            return WrapResultInJson(await _verificationService.VerifyInterpreter(officialInterpreterId, orderId.Value, competenceLevel));
        }

        private JsonResult WrapResultInJson(VerificationResult result)
        {
            return Json(new VerificationResultModel
            {
                Value = result,
                Description = EnumHelper.GetDescription(result),
            });
        }
    }
}