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
using Tolk.BusinessLogic.Entities;
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
        private readonly INotificationService _notificationService;
        private static HttpClient client = new HttpClient();
        public VerificationService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            ITolkBaseOptions tolkBaseOptions,
            ILogger<VerificationService> logger,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _tolkBaseOptions = tolkBaseOptions;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<VerificationResult> VerifyInterpreter(string interpreterId, int orderId, CompetenceAndSpecialistLevel competenceLevel)
        {
            if (string.IsNullOrWhiteSpace(interpreterId))
            {
                return VerificationResult.NotFound;
            }
            try
            {
                var order = await _dbContext.Orders
                        .Include(o => o.Language)
                        .SingleAsync(o => o.OrderId == orderId);
                if (string.IsNullOrEmpty(order.Language.TellusName))
                {
                    return VerificationResult.LanguageNotRegistered;
                }
                TellusInterpreterResponse information;

                var response = await client.GetAsync($"{_tolkBaseOptions.Tellus.Uri}{interpreterId}");
                string content = await response.Content.ReadAsStringAsync();
                information = JsonConvert.DeserializeObject<TellusInterpreterResponse>(content);

                return CheckInterpreter(competenceLevel, order, information);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to verify the interpreter against {_tolkBaseOptions.Tellus.Uri}");
                return VerificationResult.UnknownError;
            }
        }

        private static VerificationResult CheckInterpreter(CompetenceAndSpecialistLevel competenceLevel, Entities.Order order, TellusInterpreterResponse information)
        {
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

        public async Task<ValidateTellusLanguageListResult> ValidateTellusLanguageList(bool notify = false)
        {
            if (!_tolkBaseOptions.Tellus.IsActivated)
            {
                return new ValidateTellusLanguageListResult
                {
                    NewLanguages = Enumerable.Empty<TellusLanguageModel>(),
                    RemovedLanguages = Enumerable.Empty<TellusLanguageModel>(),
                };
            }
            var response = await client.GetAsync(_tolkBaseOptions.Tellus.LanguagesUri);
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TellusLanguagesResponse>(content);

            if (result.Status != 200)
            {
                if (notify)
                {
                    _notificationService.CreateEmail(_tolkBaseOptions.SupportEmail,
                        $"Verifieringen av språklistan mot Tellus misslyckades!",
                        "Det borde stå vad som gick fel här, om vi vet det...");
                }
                _logger.LogWarning($"Verifieringen av språklistan mot Tellus misslyckades, med status {result.Status}");
                return null;
            }
            var tellusLanguages = result.Result.Where(t => !_tolkBaseOptions.Tellus.UnusedIsoCodesList.Contains(t.Id)).ToList();
            var currentLanguages = _dbContext.Languages.ToList();
            var validationResult = new ValidateTellusLanguageListResult
            {
                NewLanguages = tellusLanguages.Where(t => !currentLanguages.Any(l => l.TellusName == t.Id && l.Active)).Select(t => new TellusLanguageModel
                {
                    Id = t.Id,
                    Value = t.Value,
                    ExistsInSystemWithoutTellusConnection = currentLanguages.Any(l => l.ISO_639_Code == t.Id && string.IsNullOrEmpty(l.TellusName)),
                    InactiveInSystem = currentLanguages.Any(l => (l.ISO_639_Code == t.Id || l.TellusName == t.Id) && !l.Active)
                }),
                RemovedLanguages = currentLanguages.Where(l => !string.IsNullOrEmpty(l.TellusName) && !tellusLanguages.Any(t => l.TellusName == t.Id) && l.Active)
                    .Select(l => new TellusLanguageModel
                    {
                        Id = l.TellusName,
                        Value = l.Name
                    })
            };
            if (notify)
            {
                if (validationResult.FoundChanges)
                {
                    _notificationService.CreateEmail(_tolkBaseOptions.SupportEmail,
                        "Det finns skillnader i systemets språklista och den i Tellus.",
                        $"Gå hit för att se vilka skillnader det var:\n\n{_tolkBaseOptions.TolkWebBaseUrl}/Language/Verify");
                    _logger.LogInformation($"There were differences between this system's and tellus' language lists. Notification sent to {_tolkBaseOptions.SupportEmail}");
                }
                else
                {
                    _logger.LogInformation("There were no differences between this system's and tellus' language lists.");
                }
            }

            return validationResult;
        }

        public async Task<UpdateLanguagesCompetenceResult> UpdateTellusLanguagesCompetenceInfo(bool notify = false)
        {
            if (!_tolkBaseOptions.Tellus.IsLanguagesCompetenceActivated)
                return new UpdateLanguagesCompetenceResult
                {
                    UpdatedLanguages = Enumerable.Empty<Language>(),
                    Message = "Hämtningen av språkkompetenser från Tellus är inte aktiverad, ändra konfigurering för att aktivera."
                };
            var response = await client.GetAsync(_tolkBaseOptions.Tellus.LanguagesCompetenceInfoUri);
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TellusLanguagesCompetenceInfoResponse>(content);

            if (result.Status != 200)
            {
                if (notify)
                {
                    _notificationService.CreateEmail(_tolkBaseOptions.SupportEmail,
                        $"Hämtningen av språkkompetenser från Tellus misslyckades!",
                        $"Här kan du testa att köra en hämtning direkt ifrån tjänsten:\n\n{_tolkBaseOptions.TolkWebBaseUrl}/Language/UpdateCompetences");
                }
                _logger.LogWarning($"Hämtningen av språkkompetenser från Tellus misslyckades, med status {result.Status}");
                return new UpdateLanguagesCompetenceResult
                {
                    UpdatedLanguages = Enumerable.Empty<Language>(),
                    Message = $"Hämtningen av språkkompetenser från Tellus misslyckades, med status {result.Status}"
                };
            }
            var currentTellusLanguages = _dbContext.Languages.Where(l => !string.IsNullOrWhiteSpace(l.TellusName)).ToList();
            var tellusLanguagesWithCompetences = result.Result.Where(t => currentTellusLanguages.Any(l => l.TellusName == t.Id && l.Active));
            if (!result.Result.Any() || !tellusLanguagesWithCompetences.Any())
            {
                return new UpdateLanguagesCompetenceResult
                {
                    UpdatedLanguages = Enumerable.Empty<Language>(),
                    Message = result.Result.Any() ? "Hämtningen innehöll endast ej aktiva språk eller Tellusnamn som inte förekommer i tjänsten" : "Hämtningen innehöll inga språk"
                };
            }
            List<Language> updatedLanguages = new List<Language>();

            foreach (TellusLanguagesInfoModel l in tellusLanguagesWithCompetences)
            {
                var updates = currentTellusLanguages.Where(ls => ls.TellusName == l.Id &&
                (l.HasLegal != ls.HasLegal ||
                l.HasHealthcare != ls.HasHealthcare ||
                l.HasAuthorized != ls.HasAuthorized ||
                l.HasEducated != ls.HasEducated)).ToList();
                updates.ForEach(c => { c.HasLegal = l.HasLegal; c.HasHealthcare = l.HasHealthcare; c.HasAuthorized = l.HasAuthorized; c.HasEducated = l.HasEducated; });
                updatedLanguages.AddRange(updates);
            }
            _dbContext.SaveChanges();
            return new UpdateLanguagesCompetenceResult { UpdatedLanguages = updatedLanguages, Message = updatedLanguages.Any() ? string.Empty : "Inga skillnader i kompetenser fanns mellan tjänsten och Tellus" };
        }

        private static VerificationResult VerifyInterpreter(DateTimeOffset startAt, IEnumerable<TellusInterpreterLevelModel> levels)
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
