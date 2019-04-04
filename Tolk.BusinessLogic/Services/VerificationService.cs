using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
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
                    var information = content != EmptyResult ? JsonConvert.DeserializeObject<TellusInterpreterModel>(content) : null;
                    if (information == null)
                    {
                        return VerificationResult.NotFound;
                    }
                    var competences = information.Competences.Where(c => c.Language == order.Language.TellusName);
                    if (competences.Any(c => c.CompetenceLevel == competenceLevel.GetTellusName()))
                    {
                        var competence = competences.Single(c => c.CompetenceLevel == competenceLevel.GetTellusName());
                        if (competence.IsValidAt(order.StartAt))
                        {
                            return VerificationResult.Validated;
                        }
                        return VerificationResult.CompetenceExpiredAtAssignment;
                    }
                    return VerificationResult.NotCorrectCompetence;
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e, $"Failed to verify the interpreter against {_tolkBaseOptions.Tellus.Uri}");
                return VerificationResult.UnknownError;
            }
        }
    }
}
