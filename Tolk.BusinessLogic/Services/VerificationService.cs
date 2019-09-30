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
        private readonly EmailService _emailService;
        private static HttpClient client = new HttpClient();
        public VerificationService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            ITolkBaseOptions tolkBaseOptions,
            ILogger<VerificationService> logger,
            INotificationService notificationService,
            EmailService emailService
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _tolkBaseOptions = tolkBaseOptions;
            _logger = logger;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<VerificationResult> VerifyInterpreter(string interpreterId, int orderId, CompetenceAndSpecialistLevel competenceLevel, bool reVerify = false)
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
                _logger.LogError(e, $"Failed to verify the interpreter against {_tolkBaseOptions.Tellus.Uri}" + (reVerify ? " for second time" : " for first time"));
                //try to verify once more if reVerify is false since it's probably most often connection problem/timeout
                return !reVerify ? await VerifyInterpreter(interpreterId, orderId, competenceLevel, true) : VerificationResult.UnknownError;
            }
        }

        private static VerificationResult CheckInterpreter(CompetenceAndSpecialistLevel competenceLevel, Order order, TellusInterpreterResponse information)
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

        public async Task HandleTellusVerifications(bool notify = false)
        {
            await ValidateTellusLanguageList(notify);
            await UpdateTellusLanguagesCompetenceInfo(notify);
        }

        public async Task<ValidateTellusLanguageListResult> ValidateTellusLanguageList(bool notify = false)
        {
            _logger.LogInformation($"Starting {nameof(ValidateTellusLanguageList)}");
            if (!_tolkBaseOptions.Tellus.IsActivated)
            {
                return new ValidateTellusLanguageListResult
                {
                    NewLanguages = Enumerable.Empty<TellusLanguageModel>(),
                    RemovedLanguages = Enumerable.Empty<TellusLanguageModel>(),
                };
            }
            try
            {
                var response = await client.GetAsync(_tolkBaseOptions.Tellus.LanguagesUri);
                string content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TellusLanguagesResponse>(content);

                if (result.Status != 200)
                {
                    _logger.LogWarning($"Verifieringen av språklistan mot Tellus misslyckades, med status {result.Status}");
                    if (notify)
                    {
                        _notificationService.CreateEmail(_tolkBaseOptions.Support.SecondLineEmail,
                            $"Verifieringen av språklistan mot Tellus misslyckades!",
                            "Det borde stå vad som gick fel här, om vi vet det...");
                    }
                    return null;
                }
                var tellusLanguages = result.Result.Where(t => !_tolkBaseOptions.Tellus.UnusedIsoCodesList.Contains(t.Id)).ToList();
                var currentLanguages = await _dbContext.Languages.ToListAsync();
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
                        _logger.LogInformation($"There were differences between this system's and tellus' language lists. Notification sent to {_tolkBaseOptions.Support.SecondLineEmail}");
                        _notificationService.CreateEmail(_tolkBaseOptions.Support.SecondLineEmail,
                            "Det finns skillnader i systemets språklista och den i Tellus.",
                            $"Gå hit för att se vilka skillnader det var:\n\n{_tolkBaseOptions.TolkWebBaseUrl}/Language/Verify");
                    }
                    else
                    {
                        _logger.LogInformation("There were no differences between this system's and tellus' language lists.");
                    }
                }

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Running {nameof(ValidateTellusLanguageList)} failed");
                SendErrorMail(nameof(ValidateTellusLanguageList), ex);
                return new ValidateTellusLanguageListResult
                {
                    NewLanguages = Enumerable.Empty<TellusLanguageModel>(),
                    RemovedLanguages = Enumerable.Empty<TellusLanguageModel>(),
                };
            }
        }

        public async Task<UpdateLanguagesCompetenceResult> UpdateTellusLanguagesCompetenceInfo(bool notify = false)
        {
            _logger.LogInformation($"Starting {nameof(UpdateTellusLanguagesCompetenceInfo)}");
            if (!_tolkBaseOptions.Tellus.IsLanguagesCompetenceActivated)
                return new UpdateLanguagesCompetenceResult
                {
                    UpdatedLanguages = Enumerable.Empty<Language>(),
                    Message = "Hämtningen av språkkompetenser från Tellus är inte aktiverad, ändra konfigurering för att aktivera."
                };
            try
            {
                var response = await client.GetAsync(_tolkBaseOptions.Tellus.LanguagesCompetenceInfoUri);
                string content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TellusLanguagesCompetenceInfoResponse>(content);

                if (result.Status != 200)
                {
                    if (notify)
                    {
                        _notificationService.CreateEmail(_tolkBaseOptions.Support.SecondLineEmail,
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
                var currentTellusLanguages = await _dbContext.Languages.Where(l => !string.IsNullOrWhiteSpace(l.TellusName)).ToListAsync();
                var tellusLanguagesWithCompetences = result.Result.Where(t => currentTellusLanguages.Any(l => l.TellusName == t.Id && l.Active));
                if (!result.Result.Any() || !tellusLanguagesWithCompetences.Any())
                {
                    _logger.LogWarning($"Result of {nameof(UpdateTellusLanguagesCompetenceInfo)} had no active languages");
                    if (notify)
                    {
                        _notificationService.CreateEmail(_tolkBaseOptions.Support.SecondLineEmail,
                            result.Result.Any() ? "Hämtningen av språkkompetenser från Tellus innehöll endast ej aktiva språk eller Tellusnamn som inte förekommer i tjänsten" : "Hämtningen av språkkompetenser från Tellus innehöll inga språk",
                            $"Här kan du testa att köra en hämtning direkt ifrån tjänsten:\n\n{_tolkBaseOptions.TolkWebBaseUrl}/Language/UpdateCompetences");
                    }
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
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Update of {nameof(UpdateTellusLanguagesCompetenceInfo)} completed");
                return new UpdateLanguagesCompetenceResult { UpdatedLanguages = updatedLanguages, Message = updatedLanguages.Any() ? string.Empty : "Inga skillnader i kompetenser fanns mellan tjänsten och Tellus" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Running {nameof(UpdateTellusLanguagesCompetenceInfo)} failed");
                SendErrorMail(nameof(UpdateTellusLanguagesCompetenceInfo), ex);
                return new UpdateLanguagesCompetenceResult
                {
                    UpdatedLanguages = Enumerable.Empty<Language>(),
                    Message = "Hämtningen av språkkompetenser misslyckades."
                };
            }
        }

        private async void SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendSupportErrorEmail(nameof(VerificationService), methodname, ex);
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
