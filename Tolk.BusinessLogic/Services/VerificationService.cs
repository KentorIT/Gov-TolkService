using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class VerificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly ITolkBaseOptions _tolkBaseOptions;
        private readonly ILogger<VerificationService> _logger;
        private const string EmptyResult = "[]";

        public VerificationService(TolkDbContext dbContext, ISwedishClock clock, ITolkBaseOptions tolkBaseOptions, ILogger<VerificationService> logger)
        {
            _dbContext = dbContext;
            _clock = clock;
            _tolkBaseOptions = tolkBaseOptions;
            _logger = logger;
        }

        public async Task<VerificationResult> VerifyInterpreter(string interpreterId, int orderId, CompetenceAndSpecialistLevel competenceLevel)
        {
            try
            {
                var order = await _dbContext.Orders
                        .Include(o => o.Language)
                        .SingleAsync(o => o.OrderId == orderId);
                if (string.IsNullOrEmpty(order.Language.TellusName))
                {
                    return VerificationResult.LanguageNotRegistered;
                }
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    var response = await client.GetAsync($"{_tolkBaseOptions.Tellus.Uri}{interpreterId}");
                    string content = await response.Content.ReadAsStringAsync();
                    var information = JsonConvert.DeserializeObject<TellusResponse>(content);

                    if (information.TotalMatching < 1)
                    {
                        return VerificationResult.NotFound;
                    }
                    var interpreter = information.Result.First();
                    if (competenceLevel == CompetenceAndSpecialistLevel.EducatedInterpreter)
                    {
                        return VerifyInterpreter(order.StartAt,
                            interpreter.Educations.Where(c => c.Language == order.Language.TellusName));
                    }
                    else
                    {
                        return VerifyInterpreter(order.StartAt,
                            interpreter.Competences.Where(c => c.Language == order.Language.TellusName &&
                                c.Competencelevel.Id == competenceLevel.GetTellusName()));
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to verify the interpreter against {_tolkBaseOptions.Tellus.Uri}");
                return VerificationResult.UnknownError;
            }
        }

        public static VerificationResult VerifyInterpreter(DateTimeOffset startAt, IEnumerable<TellusInterpreterLevelModel> levels)
        {
            if (levels.Any())
            {
                if (levels.Any(e => e.IsValidAt(startAt)))
                {
                    return VerificationResult.Validated;
                }
                return VerificationResult.CompetenceExpiredAtAssignment;
            }
            return VerificationResult.NotCorrectCompetence;
        }
    }
}
